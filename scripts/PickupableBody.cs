using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class PickupableBody : RigidBody3D, IInteractable
{
	#region Regular Variables
	private bool _IsHeld = false;
	public bool IsHeld
	{
		get { return _IsHeld; }
		set {SetIsHeld(value);}
	}
	private bool _IsSelected = false;
	public bool IsSelected
	{
		get { return _IsSelected; }
		set {SetIsSelected(value);}
	}
	private bool _CanBeSelected = true;
	public bool CanBeSelected
	{
		get { return _CanBeSelected; }
		set {SetCanBeSelected(value);}
	}
	private Node3D _HoldTarget = null;
	public Node3D HoldTarget
	{
		get {return _HoldTarget;}
		set {SetHoldTarget(value);}
	}
	public Player HoldingPlayer {set; get;} = null;

	#endregion

	#region Exported Variables
	// When true, the InvertedHullMeshes will be visible.
	private bool _OutlineVisible = false;
	[Export] public bool OutlineVisible
	{
		get { return _OutlineVisible; }
		set
		{
			_OutlineVisible = value;

			if (InvertedHullMeshes == null)
			{
				return;
			}

			for (int i = 0; i < InvertedHullMeshes.Count; i++)
			{
				MeshInstance3D InvertedHullMesh = InvertedHullMeshes[i];
				if (InvertedHullMesh == null)
				{
					continue;
				}
				InvertedHullMesh.Visible = _OutlineVisible;
			}
		}
	}
	// The meshes that consist of the inverted hull outline of the body.
	[Export] public Array<MeshInstance3D> InvertedHullMeshes { get; set; } = new Array<MeshInstance3D>();
	// The name of the body that will be displayed when it is selected.
	[Export] public string InteractableDisplayName {get; set;}
	// How tightly the body will follow the player's hand when picked up.
	[Export] public float HoldSpring = 0.5f;
	// The maximum distance the body will be allowed to be from the player's hand when picked up.
	[Export] public float MaxHoldDistance = 0.5f;
	// How much the player's speed will be scaled by when holding a body.
	[Export] public float PlayerSpeedMultiplier = 1.0f;
	[Export] public Label3D DebugLabel1;
	[Export] public Label3D DebugLabel2;
	[Export] public Label3D DebugLabel3;

	#endregion


	public override void _Ready()
	{
		CanSleep = false;
		OutlineVisible = false;
		UpdateDebugLabels();
	}

    public override void _Process(double delta)
    {
        base._Process(delta);
    }


    public override void _IntegrateForces(PhysicsDirectBodyState3D State)
    {
        if (!IsHeld || HoldTarget == null)
		{
			return;
		}
		else
		{
			if (State.Transform.Origin.DistanceTo(HoldTarget.GlobalPosition) >= MaxHoldDistance)
			{
				HoldTarget = null;
				CanBeSelected = true;
				IsHeld = false;
			}
			Vector3 ToTargetVector = HoldTarget.GlobalPosition - State.Transform.Origin;
			State.LinearVelocity = ToTargetVector * HoldSpring;
		}

    }

	public void SetIsSelected(bool Value)
	{
		_IsSelected = Value;
		OutlineVisible = _IsSelected;
		UpdateDebugLabels();
	}

	public void SetIsHeld(bool Value)
	{
		_IsHeld = Value;
		UpdateDebugLabels();
	}

	public void SetCanBeSelected(bool Value)
	{
		_CanBeSelected = Value;
		UpdateDebugLabels();
	}

	private void UpdateDebugLabels()
	{
		if (DebugLabel1 != null)
		{
			DebugLabel1.Text = "is held: " + _IsHeld;
		}
		if (DebugLabel2 != null)
		{
			DebugLabel2.Text = "is selected: " + _IsSelected;
		}
		if (DebugLabel3 != null)
		{
			DebugLabel3.Text = "can be selected: " + _CanBeSelected;
		}
	}

	public void SetHoldTarget(Node3D Value)
	{
		_HoldTarget = Value;
	}

	public override string ToString()
	{
		return Name + "#" + GetInstanceId();
	}

}
