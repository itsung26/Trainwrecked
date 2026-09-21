using Godot;
using System;

public partial class Player : CharacterBody3D
{
	#region Regular Variables
	// The interactable that the player is currently looking at. Can be a button, a holdable object, etc.
	public Interactable CurrentSelection { get; private set; }
	#endregion

	#region Exported Variables
	[Export] public Node3D PlayerCameraPivot { get; set; }
	[Export] public Camera3D PlayerCamera { get; set; }
	[Export] public StateMachine LocomotionStateMachine { get; set; }
	[Export] public RayCast3D InteractRaycast { get; set; }
	[Export] public Label3D PlayerIdLabel { get; set; }
	/// <summary>Level session chat; used to block input while typing.</summary>
	[Export] public SessionChat SessionChat { get; set; }
	[Export] public Node3D PlayerModelTreeRoot { get; set; }
	[Export] public bool LoggingDebug = false;
	[Export] public float Speed = 5.0f;
	[Export] public float GlobalSpeedModifier { get; set; } = 1.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 1.0f;
	[Export] public float SprintSpeedMultiplier = 1.5f;
	[Export] public bool InputDisabled = false;
	[Export] public bool LookDisabled = false;

	#endregion

	#region Signals
	

	#endregion

	// MultiplayerSpawner requires authority changes here (not in _Ready / after AddChild),
	// or MultiplayerSynchronizer fails to process the pending spawn.
	public override void _EnterTree()
	{
		if (int.TryParse(Name, out int peerId))
		{
			SetMultiplayerAuthority(peerId);
		}
	}

	public override void _Ready()
	{
		// SessionChat lives on the level, not under the player scene.
		SessionChat ??= GetParent()?.GetNodeOrNull<SessionChat>("SessionChat");
		PlayerIdLabel.Text = Name;
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		Input.SetMouseMode(Input.MouseModeEnum.Captured);
		// Initialize locomotion state
		if (!IsOnFloor())
		{
			LocomotionStateMachine.EnterState("FallingState");
		}
		else
		{
			LocomotionStateMachine.EnterState("GroundedState");
		}
		PlayerIdLabel.Visible = false;
		PlayerModelTreeRoot.Visible = false;
	}


	public override void _Input(InputEvent NewEvent)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		if (NewEvent is InputEventMouseMotion && !LookDisabled)
		{
			// store the relative movement of the mouse from the last frame
			InputEventMouseMotion NewMouseMotionEvent = NewEvent as InputEventMouseMotion;
			Vector2 RelativeMovement = NewMouseMotionEvent.Relative;

			float NewPlayerRotation = Rotation.Y - RelativeMovement.X * MouseSensitivity / 10.0f;
			Rotation = new Vector3(Rotation.X, NewPlayerRotation, Rotation.Z);

			float NewCameraPivotRotation = PlayerCameraPivot.Rotation.X - RelativeMovement.Y * MouseSensitivity / 10.0f;
			PlayerCameraPivot.Rotation = new Vector3(Math.Clamp(NewCameraPivotRotation, -1.5f, 1.5f), PlayerCameraPivot.Rotation.Y, PlayerCameraPivot.Rotation.Z);

		}

		else if (NewEvent is InputEventKey NewKeyEvent)
		{

		}
	}

	public override void _Process(double delta)
	{
		// Skip player logic if this player is a peer puppet.
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		// Restrict input if typing in chat detected.
		if (SessionChat is not null)
		{
			bool typing = SessionChat.IsTyping;
			InputDisabled = typing;
			LookDisabled = typing;
		}

		UpdateInteractableSelection();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}
		
		Vector3 velocity = Velocity;

		State CurrentState = LocomotionStateMachine.CurrentState;
		if (CurrentState == null)
		{
			return;
		}

		String CurrentLocomotionStateName = CurrentState.Name;

		if (!IsOnFloor())
		{
			LocomotionStateMachine.EnterState("FallingState");
		}
		else if (Input.IsActionPressed("Sprint") && !InputDisabled)
		{
			LocomotionStateMachine.EnterState("SprintingState");
		}
		else
		{
			LocomotionStateMachine.EnterState("GroundedState");
		}



		#region Locomotion State Logic
		if (CurrentLocomotionStateName == "FallingState")
		{
			velocity += GetGravity() * (float)delta;
		}

		if (CurrentLocomotionStateName == "GroundedState")
		{
			float GroundedSpeed = Speed * GlobalSpeedModifier;
			Vector2 inputDir = Input.GetVector("Left", "Right", "Forwards", "Backwards");
			if (InputDisabled)
			{
				inputDir = Vector2.Zero;
			}
			Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			if (direction != Vector3.Zero)
			{
				velocity.X = direction.X * GroundedSpeed;
				velocity.Z = direction.Z * GroundedSpeed;
			}
			else
			{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, GroundedSpeed);
				velocity.Z = Mathf.MoveToward(Velocity.Z, 0, GroundedSpeed);
			}

			// Handle jump AFTER lateral movement to avoid b-hopping.
			if (Input.IsActionJustPressed("Jump") && !InputDisabled)
			{
				velocity.Y = JumpVelocity;
			}
		}

		if (CurrentLocomotionStateName == "SprintingState")
		{
			float SprintSpeed = Speed * SprintSpeedMultiplier * GlobalSpeedModifier;
			Vector2 inputDir = Input.GetVector("Left", "Right", "Forwards", "Backwards");
			if (InputDisabled)
			{
				inputDir = Vector2.Zero;
			}
			Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			if (direction != Vector3.Zero)
			{
				velocity.X = direction.X * SprintSpeed;
				velocity.Z = direction.Z * SprintSpeed;
			}
			else
			{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, SprintSpeed);
				velocity.Z = Mathf.MoveToward(Velocity.Z, 0, SprintSpeed);
			}

			// Handle jump AFTER lateral movement to avoid b-hopping.
			if (Input.IsActionJustPressed("Jump") && !InputDisabled)
			{
				velocity.Y = JumpVelocity;
			}
		}

		#endregion


		Velocity = velocity;
		MoveAndSlide();
	}

	public void UpdateInteractableSelection()
	{
		Interactable hitInteractable = InteractRaycast.GetCollider() as Interactable;

		// prevent reselecting the same thing
		if (CurrentSelection.Equals(hitInteractable))
		{
			return;
		}

		// looking off of a selected object into nothing
		if (hitInteractable is null && CurrentSelection is not null)
		{
			CurrentSelection.Selected = false;

			CurrentSelection = hitInteractable;
		}
		// Selected object -> different selected object
		// looking from a selected object onto a different object
		else if (hitInteractable is not null && CurrentSelection is not null)
		{
			CurrentSelection.Selected = false;
			hitInteractable.Selected = true;
			
			CurrentSelection = hitInteractable;
		}
		// looking onto a selected object from nothing
		else if (hitInteractable is not null && CurrentSelection is null)
		{
			hitInteractable.Selected = true;

			CurrentSelection = hitInteractable;
		}
	}

}
