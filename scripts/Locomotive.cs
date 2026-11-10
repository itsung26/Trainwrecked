using Godot;
using System;

[Tool, GlobalClass, Icon("res://assets/2d assets/locomotive_icon.svg")]
public partial class Locomotive : RollingStock
{
    public override void _Ready()
    {
        base._Ready();
        HasPropulsion = true;
    }
}
