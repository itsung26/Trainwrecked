using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool, GlobalClass, Icon("res://assets/2d assets/train_consist_icon.svg")]
public partial class TrainConsist : Node
{
    [Export] public TrackPath Track { get; set; }
    private float _headOffset;
    [Export]
    public float HeadOffset
    {
        get => _headOffset;
        set => SetHeadOffset(value);
    }
    [Export] public float Velocity { get; set; }
    [Export] public float Damping { get; set; } = 1f;
    [Export] public bool CanCoupleFront { get; set; } = true;
    [Export] public bool CanCoupleRear { get; set; } = true;
    public RollingStock Head => GetHead();
    public RollingStock Tail => GetTail();
    /// When accessed, returns a copy of all RollingStock children. Cannot be modified.
    public Array<RollingStock> Cars => GetAllCars();

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        ChildEnteredTree += _on_child_entered_tree;
        ChildOrderChanged += _on_child_order_changed;
        ChildExitingTree += _on_child_exiting_tree;

        RebuildCouplingLinks();

        if (!NetworkManager.IsServer())
        {
            return;
        }

        PlaceCars();
    }

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
        HeadOffset += Velocity * (float)delta;
        PlaceCars();
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        ChildEnteredTree -= _on_child_entered_tree;
        ChildOrderChanged -= _on_child_order_changed;
        ChildExitingTree -= _on_child_exiting_tree;
    }

    private void SetHeadOffset(float value)
    {
        _headOffset = value;

        if (Head is null)
        {
            return;
        }

        if (Track is null)
        {
            Debug.LogWarn("No track set, returning.");
            return;
        }

        Array<RollingStock> cars = Cars;
        float headToLastCouplerDistance = cars[0].RearCouplerDistance();
        for (int i = 1; i < cars.Count; i++)
        {
            headToLastCouplerDistance += cars[i].CouplerSpan();
        }

        _headOffset = Mathf.Clamp(value, headToLastCouplerDistance, Track.Length() - Head.FrontCouplerDistance());

    }

    private RollingStock GetHead()
    {
        if (Cars.Count == 0)
        {
            return null;
        }

        RollingStock head = Cars[0];

        return head;
    }

    private RollingStock GetTail()
    {
        if (Cars.Count == 0)
        {
            return null;
        }
        
        RollingStock tail = Cars[Cars.Count - 1];

        return tail;
    }

    /// Doubly linked references are determined by child order.
    private void RebuildCouplingLinks()
    {
        Array<RollingStock> cars = Cars;
        for (int i = 0; i < cars.Count; i++)
        {
            RollingStock current = cars[i];
            current.Ahead = i > 0 ? cars[i - 1] : null;
            current.Behind = i < cars.Count - 1 ? cars[i + 1] : null;
        }
    }

    private void PlaceCars()
    {
        float offset = HeadOffset;
        for (int i = 0; i < Cars.Count; i++)
        {
            Cars[i].GlobalTransform = Track.GetPathTransformAtOffset(offset);
            if (i + 1 >= Cars.Count)
            {
                break;
            }
            offset -= Cars[i].RearCouplerDistance() + Cars[i + 1].FrontCouplerDistance();
        }
    }

    public float GetCombinedRollingMass()
    {
        float total = 0f;
        Array<RollingStock> cars = Cars;

        foreach (RollingStock car in cars)
        {
            total += car.RollingMass;
        }

        return total;
    }

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

    private void DampenVelocity(double delta)
    {

        float combinedMass = GetCombinedRollingMass();

        if (combinedMass > 0f && Damping > 0f)
        {
            Velocity *= Mathf.Exp(-Damping * (float)delta / combinedMass);
        }
    }    

    /// Returns the full arc length of the train, from frontmost coupler to back coupler.
    public float Length()
    {
        if (Cars.Count == 0)
        {
            throw new InvalidOperationException("Cannot measure length; consist has no cars.");
        }

        if (Cars.Count == 1)
        {
            return Head.CouplerSpan();
        }

        float totalSpan = 0.0f;
        RollingStock current = Head;
        while (current is not null)
        {
            totalSpan += current.CouplerSpan();
            current = current.Behind;
        }

        return totalSpan;
    }

    public void Split(int at)
    {
        if (!NetworkManager.IsServer())
        {
            return;
        }

        Array<RollingStock> cars = Cars;

        if (at < 0 || at > cars.Count)
        {
            throw new IndexOutOfRangeException($"Split index {at} is outside [0, {cars.Count}].");
        }

        // Skip splitting at ends.
        if (at == 0 || at == cars.Count)
        {
            return;
        }

        if (string.IsNullOrEmpty(SceneFilePath))
        {
            throw new InvalidOperationException($"{Name} has no SceneFilePath; cannot Split.");
        }

        PackedScene packedScene = GD.Load<PackedScene>(SceneFilePath);
        if (packedScene is null)
        {
            throw new InvalidOperationException($"Failed to load consist scene '{SceneFilePath}'.");
        }

        TrainConsist newConsist = packedScene.Instantiate<TrainConsist>();

        // Packed cars are the original scene lineup, not this live consist's cars.
        Array<RollingStock> placeholderCars = newConsist.Cars;
        for (int i = placeholderCars.Count - 1; i >= 0; i--)
        {
            placeholderCars[i].Free();
        }

        float frontCarsSpan = 0.0f;
        for (int i = 0; i < at; i++)
        {
            frontCarsSpan += cars[i].CouplerSpan();
        }

        float rearHeadOffset = HeadOffset - frontCarsSpan;

        // Snapshot before reparenting mutates this consist's children.
        List<RollingStock> rearCars = new List<RollingStock>();
        for (int i = at; i < cars.Count; i++)
        {
            rearCars.Add(cars[i]);
        }

        newConsist.Track = Track;
        newConsist.Velocity = Velocity;
        newConsist.Damping = Damping;
        newConsist.CanCoupleFront = CanCoupleFront;
        newConsist.CanCoupleRear = CanCoupleRear;

        GetTree().CurrentScene.AddChild(newConsist, true);

        foreach (RollingStock car in rearCars)
        {
            car.Reparent(newConsist);
        }

        newConsist.HeadOffset = rearHeadOffset;

        RebuildCouplingLinks();
        newConsist.RebuildCouplingLinks();
    }

    /// Merges {this} consist into {with}, reparenting children cars.
    /// Original empty consist is freed. Index is an insertion index.
    /// 0 is at the very front. Cars.count is at the very back.
    /// 1 inserts {this} consist between the frontmost car and the second car.
    /// The car calling this will be freed, merging into {with}.
    public void Join(TrainConsist with, int insertionIndex)
    {
        ArgumentNullException.ThrowIfNull(with);

        if (!NetworkManager.IsServer())
        {
            return;
        }

        if (with == this)
        {
            throw new InvalidOperationException("Cannot join a consist with itself.");
        }

        Array<RollingStock> destinationCars = with.Cars;

        if (insertionIndex < 0 || insertionIndex > destinationCars.Count)
        {
            throw new IndexOutOfRangeException($"Join insertion index {insertionIndex} is outside [0, {destinationCars.Count}].");
        }

        Array<RollingStock> cars = Cars;

        // Snapshot before reparenting mutates this consist's children.
        List<RollingStock> joiningCars = new List<RollingStock>();
        for (int i = 0; i < cars.Count; i++)
        {
            joiningCars.Add(cars[i]);
        }

        if (joiningCars.Count == 0)
        {
            QueueFree();
            return;
        }

        bool insertAtFront = insertionIndex == 0;
        bool insertAtRear = insertionIndex == destinationCars.Count;

        if (insertAtFront)
        {
            with.CanCoupleFront = CanCoupleFront;
        }

        if (insertAtRear)
        {
            with.CanCoupleRear = CanCoupleRear;
        }

        foreach (RollingStock car in joiningCars)
        {
            car.Reparent(with);
        }

        for (int i = 0; i < joiningCars.Count; i++)
        {
            with.MoveChild(joiningCars[i], insertionIndex + i);
        }

        if (insertAtFront)
        {
            with.HeadOffset = HeadOffset;
        }

        QueueFree();
    }

    private void _on_child_entered_tree(Node node)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        if (node is not RollingStock)
        {
            Debug.LogWarn("Non RollingStock child added to TrainConsist.");
            return;
        }

        RebuildCouplingLinks();
    }

    private void _on_child_order_changed()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        
        RebuildCouplingLinks();
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

        // Exiting car is still in Cars until this signal returns; rebuild after it leaves.
        CallDeferred(MethodName.RebuildCouplingLinks);
    }

}
