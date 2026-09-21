using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class ButtonInteractable : Interactable
{
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }
    
    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
    }

}
