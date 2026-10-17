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
    /// <summary>Whether another consist can couple to <see cref="Head"/>.</summary>
    [Export] public bool CanCoupleFront { get; set; } = true;
    /// <summary>
    /// Whether another consist can couple to <see cref="Tail"/>.
    /// A caboose consist sets this to <see langword="false"/>.
    /// </summary>
    [Export] public bool CanCoupleRear { get; set; } = true;
    public RollingStock Head { get; private set; }
    public RollingStock Tail { get; private set; }
    public Array<RollingStock> Cars { get; private set; }

    public override void _Ready()
    {
        ChildEnteredTree += _on_child_entered_tree;
        ChildOrderChanged += _on_child_order_changed;
        // Fire the signal to populate Cars on initial load.
        _on_child_order_changed();

        ValidateTrain();
        Head = GetChild<RollingStock>(0);
        Tail = GetChild<RollingStock>(GetChildCount() - 1);
    }

    public override void _PhysicsProcess(double delta)
    {
        DampenVelocity(delta);

    }

    private void PlaceCar(RollingStock which)
    {
        if (which is null || Track is null || Track.Curve.GetBakedLength() <= 0f)
        {
            return;
        }

        
    }

    /// Places all cars on the track in order.
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

    private void DampenVelocity(double delta)
    {     
        float combinedMass = GetCombinedRollingMass();

        if (combinedMass > 0f && Damping > 0f)
        {
            Velocity *= Mathf.Exp(-Damping * (float)delta / combinedMass);
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

    private void _on_child_order_changed()
    {
        Array<RollingStock> cars = new Array<RollingStock>();
        foreach (Node child in GetChildren())
        {
            if (child is RollingStock car)
            {
                cars.Add(car);
            }
        }

        Cars = cars;
    }

}
