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
/// On the host, each physics frame damps <see cref="Velocity"/>, adds that same arc step to every car's <see cref="RollingStock.TrackOffset"/>, and places each car from its own offset.
/// The step is shortened so the outermost coupler stays on the baked curve. <see cref="Velocity"/> is cleared when the chain is already against the end it is moving toward.
/// </remarks>
[Tool, GlobalClass, Icon("res://assets/2d assets/train_consist_icon.svg")]
public partial class TrainConsist : Node
{
    /// <summary>Curve this consist rides. Offsets are arc lengths on its current bake.</summary>
    [Export] public TrackPath Track { get; set; }
    [Export] public float StartingOffset { get; set; }
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
    public RollingStock Head => GetHead();
    /// <summary>Last child, captured in <see cref="_Ready"/>. Not refreshed if the child list changes.</summary>
    public RollingStock Tail => GetTail();
    /// <summary><see cref="RollingStock"/> children in child order. Returns <see cref="GetAllCars"/>.</summary>
    public Array<RollingStock> Cars => GetAllCars();

    /// <summary>
    /// Validates the chain, captures <see cref="Head"/> and <see cref="Tail"/>, and snaps every car onto <see cref="Track"/>.
    /// </summary>
    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        ChildEnteredTree += _on_child_entered_tree;
        ChildExitingTree += _on_child_exiting_tree;

        if (!NetworkManager.IsServer())
        {
            return;
        }

        if (Cars.Count == 0)
        {
            QueueFree();
        }

    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            Transform3D sampled = Track.GetPathTransformAtOffset(StartingOffset);
            Cars[0].GlobalTransform = sampled;
        }
    }

    /// <summary>
    /// Damps <see cref="Velocity"/>, advances every <see cref="RollingStock.TrackOffset"/> by the same limited step, then places each car.
    /// </summary>
    /// <param name="delta">Physics frame time, in seconds.</param>
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        if (!NetworkManager.IsServer())
        {
            return;
        }

        DampenVelocity(delta);
        PlaceAllCars();
    }

    public override void _Input(InputEvent @event)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        if (Input.IsActionJustPressed("Debug Action"))
        {
            int middle = (0 + Cars.Count - 1) / 2;
            // Cars[middle].QueueFree();
            Split(middle);
        }
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        ChildEnteredTree -= _on_child_entered_tree;
        ChildExitingTree -= _on_child_exiting_tree;
    }

    private RollingStock GetHead()
    {
        RollingStock head = Cars[0];

        return head;
    }

    private RollingStock GetTail()
    {
        RollingStock tail = Cars[Cars.Count - 1];

        return tail;
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

    /// Splits this consist into two consists at index <param name="at">.
    /// Follows the same index splitting conventions as strings.
    /// For example, splitting at index 1 will split the head car from the
    /// rest of the consist.
    public void Split(int at)
    {
        if(!NetworkManager.IsServer())
        {
            return;
        }

        if (at < 0 || at > Cars.Count)
        {
            throw new IndexOutOfRangeException();
        }

        if (at == 0 || at == Cars.Count)
        {
            return;
        }

        TrainConsist newConsist = Duplicate() as TrainConsist;
        foreach (RollingStock car in newConsist.Cars)
        {
            car.Free();
        }
        GetTree().CurrentScene.AddChild(newConsist);

        Array<RollingStock> cars = Cars;
        for (int i = at; i < cars.Count; i++)
        {
            cars[i].Reparent(newConsist);
        }


    }

    /// Consist <param name="with"> will be joined with <this>, not the other way around.
    /// If <paramref name="atFront"/> is true, the back of <paramref name="with"/> will be attatched to
    /// the front of <this>.
    public void Join(TrainConsist with, bool atFront)
    {
        ArgumentNullException.ThrowIfNull(with);

        if (!NetworkManager.IsServer())
        {
            return;
        }
    }

    private void _on_child_entered_tree(Node node)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        RollingStock newCarChild = node as RollingStock;

        if (newCarChild is null)
        {
            Debug.LogWarn("Non RollingStock child added to TrainConsist.");
        }
    }

    private void _on_child_exiting_tree(Node node)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        
        if (node is not RollingStock exitingCar)
        {
            return;
        }

        Array<RollingStock> cars = Cars;
        int indexOfExitingCar = cars.IndexOf(exitingCar);
        if (indexOfExitingCar < 0)
        {
            return;
        }

        exitingCar.Ahead = null;
        exitingCar.Behind = null;

        // Sole car: nothing left to couple; drop the empty consist.
        if (cars.Count == 1)
        {
            // Spawner replicates removing this consist root; clients must not free locally.
            if (NetworkManager.IsServer())
            {
                QueueFree();
            }
            return;
        }

        int beforeIndex = indexOfExitingCar - 1;
        int afterIndex = indexOfExitingCar + 1;

        // Front car: new head has nothing ahead.
        if (indexOfExitingCar == 0)
        {
            cars[afterIndex].Ahead = null;
            return;
        }

        // Back car: new tail has nothing behind.
        if (indexOfExitingCar == cars.Count - 1)
        {
            cars[beforeIndex].Behind = null;
            return;
        }

        // Middle car: bridge the gap.
        cars[beforeIndex].Behind = cars[afterIndex];
        cars[afterIndex].Ahead = cars[beforeIndex];
    }

}
