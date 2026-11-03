using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

/// <summary>
/// A connected chain of <see cref="RollingStock"/> children on one <see cref="TrackPath"/>.
/// A single child is a lone car.
/// </summary>
/// <remarks>
/// Child order is the chain: child 0 is <see cref="Head"/> and the last child is <see cref="Tail"/>.
/// Those references are captured in <see cref="_Ready"/> and are not refreshed if the child list changes.
/// They name the ends of the chain. They are not the source of the other cars' positions.
/// Cars in one consist are already coupled to their neighbors. Another consist joins this one only at an end whose coupler flag is set.
/// On the host, each physics frame damps <see cref="Velocity"/>, adds that same arc step to every car's <see cref="RollingStock.TrackOffset"/>, and places each car from its own offset.
/// The step is shortened so the outermost coupler stays on the baked curve. <see cref="Velocity"/> is cleared when the chain is already against the end it is moving toward.
/// Clients keep a buffer of those offsets and render a few ticks behind the newest sample, advancing a local playback head rather than snapping to packet arrival.
/// </remarks>
[GlobalClass, Icon("res://assets/2d assets/train_consist_icon.svg")]
public partial class TrainConsist : Node
{
    /// <summary>Curve this consist rides. Offsets are arc lengths on its current bake.</summary>
    [Export] public TrackPath Track { get; set; }
    /// <summary>
    /// Shared arc-length speed, in units per second along <see cref="Track"/>.
    /// Positive speed increases every <see cref="RollingStock.TrackOffset"/>.
    /// </summary>
    [Export] public float Velocity { get; set; }
    /// <summary>
    /// Linear damping rate for <see cref="Velocity"/>, in inverse seconds at a combined mass of <c>1</c>.
    /// </summary>
    /// <remarks>
    /// Intended decay is <c>exp(-Damping * delta / mass)</c>, where mass is <see cref="GetCombinedRollingMass"/>.
    /// A heavier consist coasts longer. <c>0</c> leaves velocity unchanged.
    /// </remarks>
    [Export] public float Damping { get; set; } = 1f;
    /// <summary>Whether another consist can couple to <see cref="Head"/>.</summary>
    [Export] public bool CanCoupleFront { get; set; } = true;
    /// <summary>
    /// Whether another consist can couple to <see cref="Tail"/>.
    /// A caboose consist sets this to <see langword="false"/>.
    /// </summary>
    [Export] public bool CanCoupleRear { get; set; } = true;
    /// <summary>Child 0, captured in <see cref="_Ready"/>. Not refreshed if the child list changes.</summary>
    public RollingStock Head { get; private set; }
    /// <summary>Last child, captured in <see cref="_Ready"/>. Not refreshed if the child list changes.</summary>
    public RollingStock Tail { get; private set; }
    /// <summary><see cref="RollingStock"/> children in child order. Returns <see cref="GetAllCars"/>.</summary>
    public Array<RollingStock> Cars => GetAllCars();
    /// <summary>Host snapshots waiting on the client, sorted by <see cref="TrainPoseSnapshot.Tick"/>.</summary>
    private readonly List<TrainPoseSnapshot> _networkTrainBuffer = new List<TrainPoseSnapshot>();
    /// <summary>Playback head on the host tick timeline. Advanced locally, not from packet arrival.</summary>
    private int _presentTick;
    private bool _hasPresentTick;
    /// <summary>How far behind the newest received host tick the client renders.</summary>
    private const int InterpolationDelayTicks = 3;
    private const int MaxBufferedSnapshots = 16;

    /// <summary>
    /// Validates the chain, captures <see cref="Head"/> and <see cref="Tail"/>, and snaps every car onto <see cref="Track"/>.
    /// </summary>
    public override void _Ready()
    {
        ChildEnteredTree += _on_child_entered_tree;

        if (!NetworkManager.IsServer())
        {
            return;
        }

        ValidateTrain();
        Head = GetChild<RollingStock>(0);
        Tail = GetChild<RollingStock>(GetChildCount() - 1);


        SnapConsistToTrack();
    }

    /// <summary>
    /// Damps <see cref="Velocity"/>, advances every <see cref="RollingStock.TrackOffset"/> by the same limited step, then places each car.
    /// Clients play back buffered host offsets instead of stepping the chain.
    /// </summary>
    /// <param name="delta">Physics frame time, in seconds.</param>
    public override void _PhysicsProcess(double delta)
    {
        if (NetworkManager.IsServer())
        {
            DampenVelocity(delta);
            AdvanceOffsets((float)delta);
            PlaceAllCars();
            Rpc(MethodName.PushTrainPoseSnapshot, GetAllCarOffsets(), (int)Engine.GetPhysicsFrames(), Velocity);
            return;
        }

        if (NetworkManager.IsClient() && NetworkManager.IsConnected())
        {
            ApplyBufferedPlayback();
        }
    }

    /// <summary>
    /// Sets <paramref name="car"/> to the baked pose at its own <see cref="RollingStock.TrackOffset"/>.
    /// </summary>
    /// <param name="car">Car to move. Ignored when <see langword="null"/> or the track has no baked length.</param>
    private void PlaceCar(RollingStock car)
    {
        if (car is null || !TryGetBakedLength(out _))
        {
            return;
        }

        car.GlobalTransform = Track.GetPathTransformAtOffset(car.TrackOffset);
    }

    /// <summary>
    /// Projects each car onto the curve, closes coupler gaps without anchoring on <see cref="Head"/>, and shifts the chain back onto the track.
    /// </summary>
    private void SnapConsistToTrack()
    {
        if (!TryGetBakedLength(out float length) || Cars.Count == 0)
        {
            return;
        }

        for (int i = 0; i < Cars.Count; i++)
        {
            Cars[i].TrackOffset = Track.GetClosestOffset(Cars[i].GlobalPosition);
        }

        ApplyCouplerSpacing();
        ShiftChainOntoTrack(length);
        PlaceAllCars();
    }

    /// <summary>
    /// Rewrites each <see cref="RollingStock.TrackOffset"/> so adjacent couplers meet, keeping the chain's average offset.
    /// </summary>
    /// <remarks>
    /// Child <c>i</c> sits ahead of child <c>i + 1</c> along increasing offset. The path pose points local <c>-Z</c> that way.
    /// </remarks>
    private void ApplyCouplerSpacing()
    {
        int count = Cars.Count;
        if (count == 0)
        {
            return;
        }

        float[] delta = new float[count];
        for (int i = 0; i < count - 1; i++)
        {
            delta[i + 1] = delta[i] + CouplerArcDelta(Cars[i], front: false) - CouplerArcDelta(Cars[i + 1], front: true);
        }

        float meanProjected = 0f;
        float meanDelta = 0f;
        for (int i = 0; i < count; i++)
        {
            meanProjected += Cars[i].TrackOffset;
            meanDelta += delta[i];
        }

        float anchor = (meanProjected - meanDelta) / count;
        for (int i = 0; i < count; i++)
        {
            Cars[i].TrackOffset = anchor + delta[i];
        }
    }

    /// <summary>
    /// Slides every offset by the same amount so both coupler extremes lie on <c>[0, length]</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The chain is longer than the baked curve.</exception>
    private void ShiftChainOntoTrack(float length)
    {
        GetChainArcExtents(out float minArc, out float maxArc);
        if (maxArc - minArc > length)
        {
            throw new InvalidOperationException("Train does not fit on the track.");
        }

        float shift = 0f;
        if (minArc < 0f)
        {
            shift = -minArc;
        }
        else if (maxArc > length)
        {
            shift = length - maxArc;
        }

        if (shift == 0f)
        {
            return;
        }

        for (int i = 0; i < Cars.Count; i++)
        {
            Cars[i].TrackOffset += shift;
        }
    }

    /// <summary>
    /// Adds the same arc step to every car, limited so the leading coupler cannot pass either end of the curve.
    /// </summary>
    private void AdvanceOffsets(float delta)
    {
        if (!TryGetBakedLength(out float length) || Cars.Count == 0)
        {
            return;
        }

        float step = Velocity * delta;
        if (step == 0f)
        {
            return;
        }

        GetChainArcExtents(out float minArc, out float maxArc);
        if (step > 0f)
        {
            float remaining = length - maxArc;
            if (remaining <= 0f)
            {
                Velocity = 0f;
                return;
            }

            step = Mathf.Min(step, remaining);
        }
        else
        {
            if (minArc <= 0f)
            {
                Velocity = 0f;
                return;
            }

            step = Mathf.Max(step, -minArc);
        }

        for (int i = 0; i < Cars.Count; i++)
        {
            Cars[i].TrackOffset += step;
        }
    }

    /// <summary>
    /// Signed distance from the car origin to a coupler along local <c>-Z</c>.
    /// Positive is toward increasing track offset once the car faces the path.
    /// </summary>
    private float CouplerArcDelta(RollingStock car, bool front)
    {
        Marker3D coupler = front ? car.FrontCoupler : car.RearCoupler;
        if (coupler == null)
        {
            return 0f;
        }

        return -car.ToLocal(coupler.GlobalPosition).Z;
    }

    /// <summary>
    /// Writes the smallest and largest coupler arc in the chain to <paramref name="minArc"/> and <paramref name="maxArc"/>.
    /// </summary>
    /// <remarks>
    /// Each car contributes both coupler arcs. A missing coupler is treated as the car origin.
    /// </remarks>
    private void GetChainArcExtents(out float minArc, out float maxArc)
    {
        minArc = float.PositiveInfinity;
        maxArc = float.NegativeInfinity;

        for (int i = 0; i < Cars.Count; i++)
        {
            RollingStock car = Cars[i];
            float frontArc = car.TrackOffset + CouplerArcDelta(car, front: true);
            float rearArc = car.TrackOffset + CouplerArcDelta(car, front: false);
            minArc = Mathf.Min(minArc, Mathf.Min(frontArc, rearArc));
            maxArc = Mathf.Max(maxArc, Mathf.Max(frontArc, rearArc));
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> and the current baked length when <see cref="Track"/> has a curve with positive length.
    /// </summary>
    /// <param name="length">Baked length, or <c>0</c> when this returns <see langword="false"/>.</param>
    private bool TryGetBakedLength(out float length)
    {
        length = 0f;
        Curve3D curve = Track?.Curve;
        if (curve == null || curve.PointCount == 0)
        {
            return false;
        }

        length = curve.GetBakedLength();
        return length > 0f;
    }

    /// <summary>Places every car in <see cref="Cars"/> from its own <see cref="RollingStock.TrackOffset"/>.</summary>
    private void PlaceAllCars()
    {
        for (int i = 0; i < Cars.Count; i++)
        {
            PlaceCar(Cars[i]);
        }
    }

    /// <summary>
    /// Throws an exception if this consist cannot roll as one chain.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void ValidateTrain()
    {
        if (GetCombinedRollingMass() <= 0f)
        {
            throw new InvalidOperationException("Invalid train detected.");
        }
    }

    /// <summary>Returns the sum of <see cref="RollingStock.RollingMass"/> over <see cref="RollingStock"/> children.</summary>
    public float GetCombinedRollingMass()
    {
        float total = 0f;

        for (int i = 0; i < GetChildCount(); i++)
        {
            if (GetChild<RollingStock>(i) is RollingStock childCar)
            {
                total += childCar.RollingMass;
            }
        }

        return total;
    }

    /// <summary>
    /// Returns the <see cref="RollingStock"/> children in child order.
    /// </summary>
    private Array<RollingStock> GetAllCars()
    {
        Array<Node> children = GetChildren();
        List<RollingStock> cars = new List<RollingStock>();
        foreach (Node child in children)
        {
            if (child is RollingStock car)
            {
                cars.Add(car);
            }
        }

        cars.Sort((a, b) => a.GetIndex().CompareTo(b.GetIndex()));

        Array<RollingStock> sorted = new Array<RollingStock>();
        foreach (RollingStock car in cars)
        {
            sorted.Add(car);
        }

        return sorted;
    }

    public Array<Transform3D> GetAllCarPoses()
    {
        Array<Transform3D> poses = new Array<Transform3D>();

        if (Cars.Count == 0)
        {
            return poses;
        }

        foreach (RollingStock car in Cars)
        {
            poses.Add(car.GlobalTransform);
        }


        return poses;
    }

    public Array<float> GetAllCarOffsets()
    {
        Array<float> offsets = new Array<float>();

        if (Cars.Count == 0)
        {
            return offsets;
        }

        foreach (RollingStock car in Cars)
        {
            offsets.Add(car.TrackOffset);
        }

        return offsets;
    }

    /// <summary>
    /// Multiplies <see cref="Velocity"/> by <c>exp(-Damping * delta / mass)</c> when both <see cref="Damping"/> and the combined mass are positive.
    /// </summary>
    /// <param name="delta">Physics frame time, in seconds.</param>
    private void DampenVelocity(double delta)
    {

        float combinedMass = GetCombinedRollingMass();

        if (combinedMass > 0f && Damping > 0f)
        {
            Velocity *= Mathf.Exp(-Damping * (float)delta / combinedMass);
        }
    }

    /// <summary>
    /// Host snapshot path. Unreliable and sequenced so late packets are dropped instead of clumped.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void PushTrainPoseSnapshot(Array<float> offsets, int hostPhysicsTick, float velocity)
    {
        if (Cars.Count == 0 || NetworkManager.IsServer() || offsets is null || offsets.Count == 0)
        {
            return;
        }

        if (_networkTrainBuffer.Count > 0 && hostPhysicsTick <= _networkTrainBuffer[_networkTrainBuffer.Count - 1].Tick)
        {
            return;
        }

        _networkTrainBuffer.Add(new TrainPoseSnapshot(hostPhysicsTick, offsets, velocity));
        if (_networkTrainBuffer.Count > MaxBufferedSnapshots)
        {
            _networkTrainBuffer.RemoveRange(0, _networkTrainBuffer.Count - MaxBufferedSnapshots);
        }
    }

    /// <summary>
    /// Advances the playback head, writes interpolated offsets onto the cars, and places them.
    /// </summary>
    private void ApplyBufferedPlayback()
    {
        if (_networkTrainBuffer.Count == 0 || Cars.Count == 0)
        {
            return;
        }

        AdvancePlaybackHead();
        TrimConsumedSnapshots();
        ApplyCarOffsets(GetInterpolatedOffsets());
        PlaceAllCars();
    }

    /// <summary>
    /// Moves <see cref="_presentTick"/> one local physics tick, steered toward <c>newest - delay</c>.
    /// </summary>
    private void AdvancePlaybackHead()
    {
        int oldest = _networkTrainBuffer[0].Tick;
        int newest = _networkTrainBuffer[_networkTrainBuffer.Count - 1].Tick;
        int target = newest - InterpolationDelayTicks;

        if (!_hasPresentTick)
        {
            _presentTick = Mathf.Clamp(target, oldest, newest);
            _hasPresentTick = true;
            return;
        }

        if (_presentTick < target)
        {
            _presentTick++;
            if (_presentTick < target)
            {
                _presentTick++;
            }
        }
        else if (_presentTick == target && _presentTick < newest)
        {
            _presentTick++;
        }

        _presentTick = Mathf.Clamp(_presentTick, oldest, newest);
    }

    /// <summary>
    /// Drops snapshots older than the one that still brackets <see cref="_presentTick"/>.
    /// </summary>
    private void TrimConsumedSnapshots()
    {
        while (_networkTrainBuffer.Count > 2 && _networkTrainBuffer[1].Tick <= _presentTick)
        {
            _networkTrainBuffer.RemoveAt(0);
        }
    }

    /// <summary>
    /// Returns one interpolated arc offset per car from <see cref="_networkTrainBuffer"/>.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="_presentTick"/> on the host tick timeline.
    /// Offsets lerp with the same <c>t</c> between the snapshots that bracket that head.
    /// Past the newest sample, offsets step by that snapshot's velocity.
    /// </remarks>
    private Array<float> GetInterpolatedOffsets()
    {
        Array<float> interpolated = new Array<float>();
        if (_networkTrainBuffer.Count == 0)
        {
            return interpolated;
        }

        TrainPoseSnapshot older = null;
        TrainPoseSnapshot newer = null;
        foreach (TrainPoseSnapshot snapshot in _networkTrainBuffer)
        {
            if (snapshot.Tick <= _presentTick && (older is null || snapshot.Tick > older.Tick))
            {
                older = snapshot;
            }

            if (snapshot.Tick >= _presentTick && (newer is null || snapshot.Tick < newer.Tick))
            {
                newer = snapshot;
            }
        }

        IReadOnlyList<float> offsets;
        if (older is null && newer is null)
        {
            return interpolated;
        }

        if (older is null)
        {
            offsets = newer.CarOffsets;
        }
        else if (newer is null)
        {
            offsets = ExtrapolateOffsets(older, _presentTick);
        }
        else if (older.Tick == newer.Tick)
        {
            offsets = older.CarOffsets;
        }
        else
        {
            float t = (_presentTick - older.Tick) / (float)(newer.Tick - older.Tick);
            int count = Mathf.Min(older.CarOffsets.Count, newer.CarOffsets.Count);
            float[] lerped = new float[count];
            for (int i = 0; i < count; i++)
            {
                lerped[i] = Mathf.Lerp(older.CarOffsets[i], newer.CarOffsets[i], t);
            }

            offsets = lerped;
        }

        foreach (float offset in offsets)
        {
            interpolated.Add(offset);
        }

        return interpolated;
    }

    /// <summary>
    /// Steps <paramref name="snapshot"/> offsets by its velocity for ticks after <see cref="TrainPoseSnapshot.Tick"/>.
    /// </summary>
    private static IReadOnlyList<float> ExtrapolateOffsets(TrainPoseSnapshot snapshot, int presentTick)
    {
        int extraTicks = presentTick - snapshot.Tick;
        if (extraTicks <= 0)
        {
            return snapshot.CarOffsets;
        }

        float step = snapshot.Velocity * extraTicks / Engine.PhysicsTicksPerSecond;
        float[] offsets = new float[snapshot.CarOffsets.Count];
        for (int i = 0; i < offsets.Length; i++)
        {
            offsets[i] = snapshot.CarOffsets[i] + step;
        }

        return offsets;
    }

    /// <summary>Writes <paramref name="offsets"/> onto <see cref="Cars"/> in child order.</summary>
    private void ApplyCarOffsets(Array<float> offsets)
    {
        if (offsets is null || offsets.Count != Cars.Count)
        {
            return;
        }

        for (int i = 0; i < Cars.Count; i++)
        {
            Cars[i].TrackOffset = offsets[i];
        }
    }

    /// Splits this consist into two consists at index <param name="at">.
    public void Split(int at)
    {
        if (at < 0 || at > Cars.Count)
        {
            throw new IndexOutOfRangeException();
        }

        if (at == 0 || at == Cars.Count)
        {
            return;
        }


    }

    public void Join(TrainConsist with, bool atFront)
    {

    }

    private void _on_child_entered_tree(Node node)
    {
        RollingStock newCarChild = node as RollingStock;

        if (newCarChild is null)
        {
            Debug.LogWarn("Non RollingStock child added to TrainConsist.");
        }
    }

    /// <summary>One host physics sample of the chain, stored on clients for playback.</summary>
    private class TrainPoseSnapshot
    {
        public int Tick { get; }
        public float Velocity { get; }
        public IReadOnlyList<float> CarOffsets { get; }

        public TrainPoseSnapshot(int tick, List<float> carOffsets, float velocity)
        {
            Tick = tick;
            CarOffsets = carOffsets;
            Velocity = velocity;
        }

        public TrainPoseSnapshot(int tick, Array<float> carOffsets, float velocity)
            : this(tick, carOffsets.ToList(), velocity)
        {
        }

        public override string ToString() => $"{nameof(TrainPoseSnapshot)} {{ Tick = {Tick}, Velocity = {Velocity}, CarOffsets = {CarOffsets} }}";
    }

}
