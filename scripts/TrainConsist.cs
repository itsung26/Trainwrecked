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
        ChildExitingTree += _on_child_exiting_tree;

        RebuildCouplingLinks();

        if (Cars.Count == 0)
        {
            Debug.LogWarn($"{Name} ready with no RollingStock children.");
        }

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
            throw new IndexOutOfRangeException("No head present.");
        }

        RollingStock head = Cars[0];

        return head;
    }

    private RollingStock GetTail()
    {
        if (Cars.Count == 0)
        {
            throw new IndexOutOfRangeException("No tail present.");
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

        for (int i = 0; i < GetChildCount(); i++)
        {
            if (GetChild<RollingStock>(i) is RollingStock childCar)
            {
                total += childCar.RollingMass;
            }
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

    /// <summary>
    /// Returns a new consist that matches this live train's cars and properties.
    /// </summary>
    /// <remarks>
    /// Instantiates an empty shell from <see cref="Node.SceneFilePath"/> (for spawn
    /// replication), removes the packed placeholder cars, then duplicates each live
    /// <see cref="RollingStock"/> in order. Coupler exports are re-bound to the
    /// duplicated subtree because <see cref="Node.Duplicate(int)"/> does not remap
    /// typed node exports reliably. The returned node is not yet in the scene tree.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Node.SceneFilePath"/> is missing or the scene fails to load.
    /// </exception>
    public TrainConsist Copy()
    {
        if (string.IsNullOrEmpty(SceneFilePath))
        {
            throw new InvalidOperationException($"{Name} has no SceneFilePath; cannot Copy.");
        }

        PackedScene packedScene = GD.Load<PackedScene>(SceneFilePath);
        if (packedScene is null)
        {
            throw new InvalidOperationException($"Failed to load consist scene '{SceneFilePath}'.");
        }

        TrainConsist copiedTrain = packedScene.Instantiate<TrainConsist>();

        // Packed cars are the original scene lineup, not this live consist's cars.
        Array<RollingStock> placeholderCars = copiedTrain.Cars;
        for (int i = placeholderCars.Count - 1; i >= 0; i--)
        {
            placeholderCars[i].Free();
        }

        Array<RollingStock> sourceCars = Cars;
        for (int i = 0; i < sourceCars.Count; i++)
        {
            RollingStock copiedCar = CopyCar(sourceCars[i]);
            copiedTrain.AddChild(copiedCar);
            copiedCar.GlobalTransform = sourceCars[i].GlobalTransform;
        }

        copiedTrain.Track = Track;
        copiedTrain.Velocity = Velocity;
        copiedTrain.Damping = Damping;
        copiedTrain.CanCoupleFront = CanCoupleFront;
        copiedTrain.CanCoupleRear = CanCoupleRear;
        copiedTrain.HeadOffset = HeadOffset;

        copiedTrain.RebuildCouplingLinks();

        return copiedTrain;
    }

    /// <summary>
    /// Duplicates <paramref name="source"/> and rebinds coupler exports to the copy's children.
    /// </summary>
    private static RollingStock CopyCar(RollingStock source)
    {
        RollingStock copy = (RollingStock)source.Duplicate();
        copy.Ahead = null;
        copy.Behind = null;
        RebindCouplerExports(source, copy);
        return copy;
    }

    /// <summary>
    /// Points <paramref name="copy"/>'s coupler exports at the duplicated nodes that
    /// correspond to <paramref name="source"/>'s coupler paths.
    /// </summary>
    private static void RebindCouplerExports(RollingStock source, RollingStock copy)
    {
        copy.FrontCoupler = source.FrontCoupler is null
            ? null
            : copy.GetNodeOrNull<CarCoupler>(source.GetPathTo(source.FrontCoupler));

        copy.RearCoupler = source.RearCoupler is null
            ? null
            : copy.GetNodeOrNull<CarCoupler>(source.GetPathTo(source.RearCoupler));
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
        if(!NetworkManager.IsServer())
        {
            return;
        }

        if (at < 0 || at > Cars.Count)
        {
            throw new IndexOutOfRangeException($"Split index {at} is outside [0, {Cars.Count}].");
        }

        // Skip splitting at ends.
        if (at == 0 || at == Cars.Count)
        {
            return;
        }

        TrainConsist newConsist = Copy();
        Array<RollingStock> copied = newConsist.Cars;
        // Strip the head (front of {at}) on the copied train; it keeps the rear.
        for (int i = at - 1; i >= 0; i--)
        {
            copied[i].Free();
        }

        // Move the headoffset to account for the new head car of the copied train.
        float frontCarsSpan = 0.0f;
        for (int i = 0; i < at; i++)
        {
            frontCarsSpan += Cars[i].CouplerSpan();
        }
        newConsist.HeadOffset -= frontCarsSpan;

        GetTree().CurrentScene.AddChild(newConsist, true);

        // Strip the rear (from {at} back) on this train; it keeps the head.
        Array<RollingStock> cars = Cars;
        for (int i = cars.Count - 1; i >= at; i--)
        {
            cars[i].Free();
        }

        RebuildCouplingLinks();
        newConsist.RebuildCouplingLinks();
    }

    public void Join(TrainConsist with, bool atFront)
    {
        ArgumentNullException.ThrowIfNull(with);

        if (!NetworkManager.IsServer())
        {
            return;
        }

        throw new NotImplementedException("TrainConsist.Join is not implemented.");
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

    private void _on_child_exiting_tree(Node node)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        if (!NetworkManager.IsServer())
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

        // Sole car: nothing left to couple.
        if (cars.Count == 1)
        {
            Debug.LogWarn($"{Name}: last RollingStock child is leaving; consist is empty.");
            return;
        }

        // Exiting car is still in Cars until this signal returns; rebuild after it leaves.
        CallDeferred(MethodName.RebuildCouplingLinks);
    }

}
