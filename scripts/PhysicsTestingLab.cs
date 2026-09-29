using Godot;
using System;

public partial class PhysicsTestingLab : TrainwreckedLevel
{
	[Export] public AnimationPlayer AnimationPlayer { get; set; }
	[Export] public string AnimationName { get; set; }

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
		}
		if (Input.IsActionJustPressed("Debug Action 2"))
		{
			GetTree().ReloadCurrentScene();
		}
    }

}
