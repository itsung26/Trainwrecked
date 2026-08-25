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
	public Vector2 UnprojectedPosition
	{
		get {}
		private set;
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		ValidateChildren();
		InitializeChild();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
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

	// Removes any non control children and extra children, keeping only the first Control.
	public void ValidateChildren()
	{
		Array<Node> Children = GetChildren();
		Array<Node> ChildrenToFree = new Array<Node>();
		bool KeptFirst = false;
		foreach (Node Child in Children)
		{
			if (!KeptFirst && Child is Control)
			{
				KeptFirst = true;
				continue;
			}
			ChildrenToFree.Add(Child);
		}
		foreach (Node ChildToFree in ChildrenToFree)
		{
			ChildToFree.Free();
		}
	}

	// Precondition: there is only one control node child
	public void InitializeChild()
	{
		
	}

	// Returns this node's 3D position as a 2D point in the viewport. 
	// Returns Vector2.Zero if the position is behind the camera.
	public Vector2 GetUnprojectedPositionFromViewport()
	{
		Camera3D ActiveCamera = GetViewport().GetCamera3D();
		if (ActiveCamera.IsPositionBehind(GlobalPosition))
		{
			return Vector2.Zero;
		}

		return ActiveCamera.UnprojectPosition(GlobalPosition);
	}
}
