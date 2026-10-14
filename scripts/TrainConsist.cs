using System;
using Godot;

/// <summary>
/// A connected chain of <see cref="RollingStock"/> children on one <see cref="TrackPath"/>.
/// A single child is a lone car.
/// </summary>
/// <remarks>
/// Child order is the chain: child 0 is <see cref="Head"/> and the last child is <see cref="Tail"/>.
/// Those references are captured in <see cref="_Ready"/> and are not refreshed if the child list changes.
/// Each physics frame, the head is held on <see cref="Track"/> and each following child is placed behind it along the curve.
/// </remarks>
[GlobalClass]
public partial class TrainConsist : Node
{
    /// <summary>The railroad this consist is held to.</summary>
    [Export] public TrackPath Track { get; set; }

    /// <summary>The first <see cref="RollingStock"/> child. Its -Z axis faces increasing curve offset.</summary>
    public RollingStock Head { get; private set; }

    /// <summary>
    /// The last <see cref="RollingStock"/> child.
    /// The same node as <see cref="Head"/> when the consist has one car.
    /// </summary>
    public RollingStock Tail { get; private set; }

    /// <summary>
    /// Sets <see cref="Head"/> to child 0 and <see cref="Tail"/> to the last child.
    /// </summary>
    /// <remarks>
    /// Throws if there are no children, or if either end is not a <see cref="RollingStock"/>.
    /// </remarks>
    public override void _Ready()
    {
        ValidateTrain();
        Head = GetChild<RollingStock>(0);
        Tail = GetChild<RollingStock>(GetChildCount() - 1);
    }


    public override void _PhysicsProcess(double delta)
    {
        // Ensure the head RollingStock stays clamped to the railroad.
        ClampHeadToTrack();
        // Ensure the connected RollingStocks stay connected in order behind the head.
        ClampFollowersToTrack();
    }

    /// <summary>
    /// Snaps child 0 onto the closest baked pose of <see cref="Track"/>.
    /// </summary>
    /// <remarks>
    /// The car origin is placed on that pose, then lifted by <see cref="RollingStock.VerticalOffset"/> along world Y.
    /// Its -Z axis faces increasing curve offset. This holds the head on the rail and does not advance it.
    /// Does nothing when there are no children or child 0 is not <see cref="RollingStock"/>.
    /// Throws <see cref="NullReferenceException"/> when <see cref="Track"/> is <see langword="null"/>.
    /// </remarks>
    private void ClampHeadToTrack()
    {
        if (Track is null)
        {
            throw new NullReferenceException("No track to clamp to.");
        }

        if (GetChildCount() == 0)
        {
            return;
        }

        if (Head is not RollingStock)
        {
            return;
        }

        PlaceOnTrack(Head, Track.GetClosestPathTransform(Head.GlobalPosition));
    }

    /// <summary>
    /// Places each <see cref="RollingStock"/> after child 0 behind the head along <see cref="Track"/>.
    /// </summary>
    /// <remarks>
    /// The head's current world position is resolved to a baked offset, then each follower's offset is that value
    /// minus the arc length from the previous car's rear coupler to this car's front coupler.
    /// Does nothing when child 0 is not <see cref="RollingStock"/> or the curve has no baked length.
    /// Throws <see cref="NullReferenceException"/> when <see cref="Track"/> is <see langword="null"/> or a coupler marker is missing.
    /// </remarks>
    private void ClampFollowersToTrack()
    {
        if (Track is null)
        {
            throw new NullReferenceException("No track to clamp to.");
        }

        if (Track.Curve is null || Track.Curve.GetBakedLength() <= 0f || GetChildCount() == 0)
        {
            return;
        }

        if (GetChild(0) is not RollingStock ahead)
        {
            return;
        }

        float offset = Track.GetClosestOffset(ahead.GlobalPosition);
        for (int i = 1; i < GetChildCount(); i++)
        {
            if (GetChild(i) is not RollingStock car)
            {
                continue;
            }

            offset -= CouplerSpan(ahead, car);
            PlaceOnTrack(car, Track.GetPathTransformAtOffset(offset));
            ahead = car;
        }
    }

    /// <summary>
    /// Returns the arc length between <paramref name="ahead"/>'s origin and <paramref name="behind"/>'s origin
    /// when the behind car's front coupler meets the ahead car's rear coupler.
    /// </summary>
    private static float CouplerSpan(RollingStock ahead, RollingStock behind)
    {
        if (ahead.RearCoupler is null || behind.FrontCoupler is null)
        {
            throw new NullReferenceException("Rolling stock is missing a coupler marker.");
        }

        float rear = ahead.ToLocal(ahead.RearCoupler.GlobalPosition).Z;
        float front = behind.ToLocal(behind.FrontCoupler.GlobalPosition).Z;
        return rear - front;
    }

    /// <summary>
    /// Places <paramref name="car"/> on <paramref name="pathTransform"/> and lifts it by <see cref="RollingStock.VerticalOffset"/>.
    /// </summary>
    private static void PlaceOnTrack(RollingStock car, Transform3D pathTransform)
    {
        Vector3 position = pathTransform.Origin;
        position.Y += car.VerticalOffset;
        car.GlobalTransform = new Transform3D(pathTransform.Basis, position);
    }

    // Throws an exception if the coupler rules would mean a train with broken connections.
    private void ValidateTrain()
    {
        bool foundIssues = false;

        for (int i = 0; i < GetChildCount(); i++)
        {
            bool isMid = !(GetChild(i) == Head) && !(GetChild(i) == Tail);
            if (isMid)
            {
                var car = GetChild<RollingStock>(i);
                if (!car.CanCoupleFront || !car.CanCoupleRear)
                {
                    foundIssues = true;
                    break;
                }
            }
        }

        if (foundIssues)
        {
            throw new InvalidOperationException("Invalid train detected.");
        }
    }
}
