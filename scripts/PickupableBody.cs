using Godot;
using System;
using Godot.Collections;

public enum HoldFaceAxis
{
	PositiveX,
	NegativeX,
	PositiveZ,
	NegativeZ
}

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
	// How tightly the body will rotate to face the chosen axis when picked up.
	[Export] public float RotationTorque = 1.0f;
	// The maximum distance the body will be allowed to be from the player's hand when picked up.
	[Export] public float MaxHoldDistance = 0.5f;
	// Which local axis of the body should face the player while held.
	[Export] public HoldFaceAxis FacePlayerAxis = HoldFaceAxis.NegativeZ;
	
	// How much the player's speed will be scaled by when holding a body.
	[Export] public float PlayerSpeedMultiplier = 1.0f;
	[Export] public Label3D DebugLabel1;
	[Export] public Label3D DebugLabel2;
	[Export] public Label3D DebugLabel3;
	[Export] public Label3D DebugLabel4;

	#endregion


	public override void _Ready()
	{
		CanSleep = false;
		OutlineVisible = false;
		UpdateDebugLabels();
	}

    public override void _Process(double delta)
    {
		UpdateDebugLabels();
    }


    public override void _IntegrateForces(PhysicsDirectBodyState3D State)
    {
        if (!IsHeld || HoldTarget == null)
		{
			return;
		}

		if (State.Transform.Origin.DistanceTo(HoldTarget.GlobalPosition) >= MaxHoldDistance)
		{
			if (HoldingPlayer != null)
			{
				HoldingPlayer.DropHeldBody();
			}
			return;
		}

		Vector3 ToTargetVector = HoldTarget.GlobalPosition - State.Transform.Origin;
		State.LinearVelocity = ToTargetVector * HoldSpring;
		ApplyFacePlayerTorque(State);
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
		if (_IsHeld)
		{
			LockRotation = false;
			Sleeping = false;
		}
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
		if (DebugLabel4 != null)
		{
			if (HoldTarget == null)
			{
				DebugLabel4.Text = "hold distance: none";
			}
			else
			{
				float Distance = GlobalPosition.DistanceTo(HoldTarget.GlobalPosition);
				DebugLabel4.Text = "hold distance: " + Distance.ToString("0.00");
			}
		}
	}

	private void ApplyFacePlayerTorque(PhysicsDirectBodyState3D State)
	{
		Vector3 DesiredFaceDirection = GetHorizontalFaceDirection(State.Transform.Origin);
		if (DesiredFaceDirection.LengthSquared() < 0.0001f)
		{
			return;
		}

		Quaternion TargetRotation = GetHeldTargetRotation(DesiredFaceDirection);
		Quaternion CurrentRotation = State.Transform.Basis.GetRotationQuaternion();
		Quaternion DeltaRotation = TargetRotation * CurrentRotation.Inverse();
		if (DeltaRotation.W < 0.0f)
		{
			DeltaRotation = -DeltaRotation;
		}

		Vector3 RotationAxis = new Vector3(DeltaRotation.X, DeltaRotation.Y, DeltaRotation.Z);
		float Angle = 2.0f * Mathf.Acos(Mathf.Clamp(DeltaRotation.W, -1.0f, 1.0f));
		if (RotationAxis.LengthSquared() < 0.000001f)
		{
			State.AngularVelocity = Vector3.Zero;
			return;
		}

		State.AngularVelocity = RotationAxis.Normalized() * Angle * RotationTorque;
	}

	private Quaternion GetHeldTargetRotation(Vector3 DesiredFaceDirection)
	{
		Vector3 LocalFaceAxis = GetLocalFaceAxis();
		Quaternion FaceAlign = new Quaternion(LocalFaceAxis, DesiredFaceDirection);
		Vector3 RotatedUp = FaceAlign * Vector3.Up;
		Vector3 DesiredUp = Vector3.Up - DesiredFaceDirection * DesiredFaceDirection.Dot(Vector3.Up);
		Vector3 RotatedUpOnFacePlane = RotatedUp - DesiredFaceDirection * DesiredFaceDirection.Dot(RotatedUp);

		if (DesiredUp.LengthSquared() < 0.0001f || RotatedUpOnFacePlane.LengthSquared() < 0.0001f)
		{
			return FaceAlign;
		}

		DesiredUp = DesiredUp.Normalized();
		RotatedUpOnFacePlane = RotatedUpOnFacePlane.Normalized();
		float TwistAngle = RotatedUpOnFacePlane.SignedAngleTo(DesiredUp, DesiredFaceDirection);
		Quaternion UpAlign = new Quaternion(DesiredFaceDirection, TwistAngle);
		return UpAlign * FaceAlign;
	}

	private Vector3 GetHorizontalFaceDirection(Vector3 BodyOrigin)
	{
		Vector3 FaceTowardPosition = HoldTarget.GlobalPosition;
		if (HoldingPlayer != null)
		{
			Camera3D PlayerCamera = HoldingPlayer.GetViewport().GetCamera3D();
			FaceTowardPosition = PlayerCamera != null ? PlayerCamera.GlobalPosition : HoldingPlayer.GlobalPosition;
		}

		Vector3 DesiredFaceDirection = FaceTowardPosition - BodyOrigin;
		DesiredFaceDirection.Y = 0.0f;
		if (DesiredFaceDirection.LengthSquared() >= 0.0001f)
		{
			return DesiredFaceDirection.Normalized();
		}

		if (HoldingPlayer != null)
		{
			Camera3D PlayerCamera = HoldingPlayer.GetViewport().GetCamera3D();
			if (PlayerCamera != null)
			{
				Vector3 CameraForward = -PlayerCamera.GlobalTransform.Basis.Z;
				CameraForward.Y = 0.0f;
				if (CameraForward.LengthSquared() >= 0.0001f)
				{
					return CameraForward.Normalized();
				}
			}
		}

		return Vector3.Zero;
	}

	private Vector3 GetLocalFaceAxis()
	{
		switch (FacePlayerAxis)
		{
			case HoldFaceAxis.PositiveX:
				return Vector3.Right;
			case HoldFaceAxis.NegativeX:
				return Vector3.Left;
			case HoldFaceAxis.PositiveZ:
				return Vector3.Back;
			default:
				return Vector3.Forward;
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
