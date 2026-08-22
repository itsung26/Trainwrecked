using Godot;
using System;

[GlobalClass]
public partial class State : Node
{
	public bool IsActive = false;
	[Export] public bool CanBeReentered = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public virtual void OnEnter()
	{
		IsActive = true;
	}

	public virtual void OnExit()
	{
		IsActive = false;
	}

	public override String ToString()
	{
		return Name + "#" + GetInstanceId();
	}
}
