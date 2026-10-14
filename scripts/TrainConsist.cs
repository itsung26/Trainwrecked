using System;
using Godot;

/// <summary>
/// A connected chain of <see cref="RollingStock"/> children on one <see cref="TrackPath"/>.
/// A single child is a lone car.
/// </summary>
/// <remarks>
/// Child order is the chain: child 0 is <see cref="Head"/> and the last child is <see cref="Tail"/>.
/// Those references are captured in <see cref="_Ready"/> and are not refreshed if the child list changes.
/// Each physics frame, only the head is held on <see cref="Track"/>. Followers are not placed yet.
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
        Head = GetChild<RollingStock>(0);
        Tail = GetChild<RollingStock>(GetChildCount() - 1);
    }


    public override void _PhysicsProcess(double delta)
    {
        // Ensure the head RollingStock stays clamped to the railroad.
        ClampHeadToTrack();
        // Ensure the connected RollingStocks stay connected in order behind the head.
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

        if (GetChild(0) is not RollingStock car)
        {
            return;
        }

        Transform3D pathTransform = Track.GetClosestPathTransform(car.GlobalPosition);
        Vector3 position = pathTransform.Origin;
        position.Y += car.VerticalOffset;

        car.GlobalTransform = new Transform3D(pathTransform.Basis, position);
    }
}
