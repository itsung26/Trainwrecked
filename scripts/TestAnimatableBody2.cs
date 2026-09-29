using Godot;
using System;

public partial class TestAnimatableBody2 : AnimatableBody3D
{
	[Export] public bool Running { get; set; } = false;
	[Export] public float Speed { get; set; }

    public override void _PhysicsProcess(double delta)
    {
		if (Input.IsActionJustPressed("Debug Action"))
		{
			Running = true;
		}

        if (Running)
		{
			Position += new Vector3(Speed * (float)delta, 0.0f, 0.0f);
		}
    }

}
