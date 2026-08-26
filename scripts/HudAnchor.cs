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
	private Vector2 _UnprojectedPosition;
	public Vector2 UnprojectedPosition
	{
		get {return GetUnprojectedPositionFromViewport();}
		private set
		{
			_UnprojectedPosition = value;
		}
	}
	// The Control child displayed at this node's unprojected screen position.
	private Control Child;
	// The Control child's scale before depth simulation is applied.
	private Vector2 BaseChildScale = Vector2.One;


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

		Child.GlobalPosition = UnprojectedPosition;
		UpdateChildScale();
		if (!IsNodeVisible())
		{
			Child.Visible = false;
		}
		else if (IsNodeVisible())
		{
			Child.Visible = true;
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
		Child = GetChild<Control>(0);
		BaseChildScale = Child.Scale;
		Child.GlobalPosition = UnprojectedPosition;
		UpdateChildScale();
	}

	// Scales the child to match world depth, or restores its authored scale.
	public void UpdateChildScale()
	{
		if (!SimulateDepth)
		{
			Child.Scale = BaseChildScale;
			return;
		}

		Camera3D ActiveCamera = GetViewport().GetCamera3D();
		float Distance = Mathf.Max(ActiveCamera.GlobalPosition.DistanceTo(GlobalPosition), 0.001f);
		float ScaleFactor = 1.0f / Distance;
		Child.Scale = BaseChildScale * ScaleFactor;
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

	// Returns true if the anchor node is visible on camera.
	public bool IsNodeVisible()
	{
		Camera3D ActiveCamera = GetViewport().GetCamera3D();

		if (ActiveCamera.IsPositionBehind(GlobalPosition))
		{
			return false;
		}

		return true;
	}
}
