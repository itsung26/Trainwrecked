using Godot;
using System;
using System.Net.Http.Headers;

/// Owns RailStock objects as children and represents a connected train of either a lone RollingStock
/// or potentially several connected RollingStocks.
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
        ClampChildrenStockToTrack();
    }

    /// Clamps all children to the track in their respective order.
    private void ClampChildrenStockToTrack()
    {
        if (Track is null)
        {
            throw new NullReferenceException("No track to clamp to.");
        }

        if (GetChildCount() == 0)
        {
            return;
        }

        RollingStock CurrentCar = Head;
        while (CurrentCar is not null)
        {
            Vector3 newPosition = new Vector3(
                Track.GetClosestPathPoint(CurrentCar.GlobalPosition).X,
                Track.GetClosestPathPoint(CurrentCar.GlobalPosition).Y + CurrentCar.VerticalOffset,
                Track.GetClosestPathPoint(CurrentCar.GlobalPosition).Z
                );
            CurrentCar.GlobalPosition = newPosition;

            CurrentCar = CurrentCar.CoupledBehind;
        }
    }

    public void Split()
    {

    }

    public void Insert()
    {
        
    }
}
