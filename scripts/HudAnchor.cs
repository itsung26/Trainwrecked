using Godot;
using System;
using Godot.Collections;
using System.Linq;

// Displays 2D UI at a position in 3D space.
[Tool]
[GlobalClass]
public partial class HudAnchor : Node3D
{
	// If true, the hud elements will be scaled to simulate world depth.
	[Export] public bool SimulateDepth { get; set; } = true;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ValidateChildren();
		InitializeChildren();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override string[] _GetConfigurationWarnings()
	{
		if (GetChildCount() == 0)
		{
			return ["HudAnchor has no UI child."];
		}

		if (GetChildCount() > 1)
		{
			return ["HudAnchor has more than one child."];
		}

		return System.Array.Empty<string>();
	}

	public override void _Notification(int What)
	{
		if (What == NotificationChildOrderChanged)
		{
			UpdateConfigurationWarnings();
		}
	}

	// Removes any non control children and ensures children are valid.
	public void ValidateChildren()
	{
		Array<Node> Children = GetChildren();
		Array<Node> ChildrenToFree = new Array<Node>();
		foreach (Node Child in Children)
		{
			if (Child is not Control)
			{
				ChildrenToFree.Add(Child);
			}
		}
		foreach (Node ChildToFree in ChildrenToFree)
		{
			ChildToFree.Free();
		}
	}

	// Precondition: all children are control nodes
	public void InitializeChildren()
	{
		
	}
}
