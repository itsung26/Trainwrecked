using Godot;
using System;

public partial class PhysicsTestingLab : TrainwreckedLevel
{
	[Export] public AnimationPlayer AnimationPlayer { get; set; }
	[Export] public string AnimationName { get; set; }
	[Export] public PathFollow3D TestPathFollow { get; set; }
	[Export] public bool Running { get; set; }
	[Export] public float Speed { get; set; }
	[Export] public AnimatableBody3D TestTrackFollowerBody { get; set; }

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

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

		if (Running)
		{
			KinematicCollision3D collision = TestTrackFollowerBody.MoveAndCollide(
				new Vector3(15.0f, 0.0f, 0.0f) * (float) delta,
				false,
				0.001f,
				false,
				5
				);
			Debug.Log(collision);
		}
    }

}
