using System;
using System.Collections.Generic;
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
/// Each physics frame damps <see cref="Velocity"/>, adds that same arc step to every car's <see cref="RollingStock.TrackOffset"/>, and places each car from its own offset.
/// The step is shortened so the outermost coupler stays on the baked curve. <see cref="Velocity"/> is cleared when the chain is already against the end it is moving toward.
/// </remarks>
[GlobalClass]
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

    /// <summary>
    /// Validates the chain, captures <see cref="Head"/> and <see cref="Tail"/>, and snaps every car onto <see cref="Track"/>.
    /// </summary>
    public override void _Ready()
    {
        if (!NetworkManager.IsServer())
        {
            return;
        }

        ChildEnteredTree += _on_child_entered_tree;

        ValidateTrain();
        Head = GetChild<RollingStock>(0);
        Tail = GetChild<RollingStock>(GetChildCount() - 1);
        
        SnapConsistToTrack();
    }

    /// <summary>
    /// Damps <see cref="Velocity"/>, advances every <see cref="RollingStock.TrackOffset"/> by the same limited step, then places each car.
    /// </summary>
    /// <param name="delta">Physics frame time, in seconds.</param>
    public override void _PhysicsProcess(double delta)
    {
        if (!NetworkManager.IsServer())
        {
            return;
        }

        DampenVelocity(delta);
        AdvanceOffsets((float)delta);
        PlaceAllCars();
        Rpc(MethodName.SyncCarPoses, GetAllCarPoses());
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
    public Array<RollingStock> GetAllCars()
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

    /// This RPC is emitted by the host and does not run locally.
    /// It recieves <param name="poses">, passed by the host and applies
    /// the list of transforms to all cars.
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void SyncCarPoses(Array<Transform3D> poses)
    {
        if (NetworkManager.IsServer() || Cars.Count == 0)
        {
            return;
        }

        for (int i = 0; i < Cars.Count; i++)
        {
            Cars[i].GlobalTransform = poses[i];
        }
    }

    private void _on_child_entered_tree(Node node)
    {
        RollingStock newCarChild = node as RollingStock;

        if (newCarChild is null)
        {
            Debug.LogWarn("Non RollingStock child added to TrainConsist.");
        }
    }

}
