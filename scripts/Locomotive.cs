using Godot;
using System;

[GlobalClass, Icon("res://assets/2d assets/locomotive_icon.svg")]
public partial class Locomotive : RollingStock
{
    public override void _Ready()
    {
        base._Ready();
        HasPropulsion = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }
}
