using System;
using Godot;

/// <summary>
/// A connected chain of <see cref="RollingStock"/> children on one <see cref="TrackPath"/>.
/// A single child is a lone car.
/// </summary>
/// <remarks>
/// Child order is the chain: child 0 is <see cref="Head"/> and the last child is <see cref="Tail"/>.
/// Those references are captured in <see cref="_Ready"/> and are not refreshed if the child list changes.
/// They name the ends of the chain. They are not the source of the other cars' positions.
/// </remarks>
[GlobalClass]
public partial class TrainConsist : Node
{
    [Export] public TrackPath Track { get; set; }
    [Export] public float Velocity { get; set; }
    /// <summary>
    /// Linear damping rate for <see cref="Velocity"/>, in inverse seconds at a combined mass of <c>1</c>.
    /// </summary>
    /// <remarks>
    /// Intended decay is <c>exp(-Damping * delta / mass)</c>, where mass is <see cref="GetCombinedRollingMass"/>.
    /// A heavier consist coasts longer. <c>0</c> leaves velocity unchanged.
    /// </remarks>
    [Export] public float Damping { get; set; } = 1f;
    public RollingStock Head { get; private set; }
    public RollingStock Tail { get; private set; }

    public override void _Ready()
    {
        ChildEnteredTree += _on_child_entered_tree;
        ValidateTrain();
        Head = GetChild<RollingStock>(0);
        Tail = GetChild<RollingStock>(GetChildCount() - 1);
    }

    /// <summary>
    /// Throws an exception if the coupler rules would mean a train with broken connections.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
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

    private void _on_child_entered_tree(Node node)
    {
        if (node is not RollingStock)
        {
            throw new InvalidOperationException("Non RollingStock child added to TrainConsist.");
        }
    }
}
