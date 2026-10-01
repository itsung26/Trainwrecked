using Godot;
using System;

public partial class PhysicsTestingLab : TrainwreckedLevel
{
	[Export] public AnimationPlayer AnimationPlayer { get; set; }
	[Export] public string AnimationName { get; set; }
	[Export] public PathFollow3D TestPathFollow { get; set; }
	[Export] public bool Running { get; set; }
	[Export] public float Speed { get; set; }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
		if (Input.IsActionJustPressed("Debug Action"))
		{
			AnimationPlayer.Play(AnimationName);
			Running = true;
		}
		if (Input.IsActionJustPressed("Debug Action 2"))
		{
			GetTree().ReloadCurrentScene();
		}

		if (Running)
		{
			TestPathFollow.ProgressRatio += Speed * (float)delta;
		}
    }

}
