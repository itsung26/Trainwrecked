using Godot;
using System;

public partial class PlayerCamera : Camera3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Position != Vector3.Zero)
		{
			GD.PushWarning("PlayerCamera is not at the origin.");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
