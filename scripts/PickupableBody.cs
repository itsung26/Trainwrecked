using Godot;
using System;
using Godot.Collections;


[GlobalClass]
public partial class PickupableBody : Interactable
{
	public enum HoldFaceAxis
	{
		PositiveX,
		NegativeX,
		PositiveZ,
		NegativeZ
	}

    public override void _Ready()
    {
        base._Ready();
    }
	
    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override void Interact()
    {
        throw new NotImplementedException();
    }

	public override string ToString()
	{
		return Debug.GenerateInstanceToString(this);
	}

}
