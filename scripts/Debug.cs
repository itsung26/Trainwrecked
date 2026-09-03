using Godot;
using System;
using Godot.Collections;

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

	public static void Log<[MustBeVariant] T>(T[] systemArrayMessage)
	{
		Array<T> godotArray = new Array<T>(systemArrayMessage);
		Log(godotArray);
	}

	public static void LogWarn<T>(T warning)
	{
		GD.PushWarning(warning.ToString());
	}

	public static void LogError<T>(T error)
	{
		GD.PushError(error.ToString());
	}

	// Returns a generated string in the form of <NODENAME#INSTANCEID>
	public static String GenerateInstanceToString(Node WhichNode)
	{
		if (WhichNode is null)
		{
			return "";
		}
		return '<' + WhichNode.Name + '#' + WhichNode.GetInstanceId() + '>';
	}
}
