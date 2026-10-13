using Godot;
using System;
using System.Net.Http.Headers;

/// Owns RailStock objects as children and represents a connected train of either a lone RollingStock
/// or potentially several connected RollingStocks.
/// Random train spawning/preconnected train spawning will be implemented later.
[GlobalClass]
public partial class TrainConsist : Node
{
    [Export] public TrackPath Track { get; set; }

    public RollingStock Head { get; private set; }
    public RollingStock Tail { get; private set; }

    public override void _Ready()
    {
        Head = GetChild<RollingStock>(0);
        Tail = GetChild<RollingStock>(GetChildCount() - 1);
    }

    public override void _Process(double delta)
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        // Ensure the head RollingStock stays clamped to the railroad.
        ClampChildStockToTrack(0);
        // Ensure the connected RollingStocks stay connected in order behind the head.
    }

    // /// Clamps all children to the track in their respective order.
    // private void ClampAllChildrenStockToTrack()
    // {
    //     int childCount = GetChildCount();
    //     for (int i = 0; i < childCount; i++)
    //     {
    //         ClampChildStockToTrack(i);
    //     }
    // }

    /// Clamps the child at childIndex to the track, including global pos and global rotation.
    /// The car's z- axis always faces the forwards direction on the track.
    private void ClampChildStockToTrack(int childIndex)
    {
        if (Track is null)
        {
            throw new NullReferenceException("No track to clamp to.");
        }

        if (childIndex < 0 || childIndex >= GetChildCount())
        {
            throw new IndexOutOfRangeException();
        }

        if (GetChild(childIndex) is not RollingStock car)
        {
            return;
        }

        Transform3D pathTransform = Track.GetClosestPathTransform(car.GlobalPosition);
        Vector3 position = pathTransform.Origin;
        position.Y += car.VerticalOffset;

        car.GlobalTransform = new Transform3D(pathTransform.Basis, position);
    }

    public void Split()
    {

    }

    public void Insert()
    {
        
    }
}
