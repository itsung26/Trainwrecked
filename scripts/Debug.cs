using Godot;
using System;

public partial class Debug : Node
{


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Debug Quit"))
		{
			GetTree().Quit();
		}
	}

	// Log a message to the console. Intended to replace Print().
	public static void Log<T>(T message)
	{
		GD.Print(message);
	}

	public static void LogWarn<T>(T warning)
	{
		GD.PushWarning(warning.ToString());
	}
}
