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
    public Array<RollingStock> Cars => GetAllCars();

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

    private void SetHeadOffset(float value)
    {
        _headOffset = value;

        if (!Engine.IsEditorHint() || Track is null || !IsInsideTree())
        {
            return;
        }

        Array<RollingStock> cars = Cars;
        if (cars.Count == 0)
        {
            return;
        }

        RollingStock firstCar = cars[0];
        firstCar.GlobalTransform = Track.GetPathTransformAtOffset(_headOffset);
        RollingStock secondCar = cars[1];
        float backwardsArcDelta = HeadOffset - firstCar.RearCouplerDistance() - secondCar.FrontCouplerDistance();
        secondCar.GlobalTransform = Track.GetPathTransformAtOffset(backwardsArcDelta);
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

    private void PlaceCar(RollingStock car)
    {

    }    


    private void PlaceAllCars()
    {
        for (int i = 0; i < Cars.Count; i++)
        {
            PlaceCar(Cars[i]);
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

    private void DampenVelocity(double delta)
    {

        float combinedMass = GetCombinedRollingMass();

        if (combinedMass > 0f && Damping > 0f)
        {
            Velocity *= Mathf.Exp(-Damping * (float)delta / combinedMass);
        }
    }

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
