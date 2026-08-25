using Godot;
using System;
using Godot.Collections;

public partial class Player : CharacterBody3D
{
	#region Private References
	// The array containing private references for validation.
	private Array<Node> PrivateReferences = new Array<Node>();
	// The node that is used to turn the camera.
	private Node3D PlayerCameraPivot;
	// The camera that is used to view the world.
	private Camera3D PlayerCamera;
	// The state machine that is used to control the player's locomotion.
	private StateMachine LocomotionStateMachine;
	// The raycast that is used to check for interactable objects.
	private RayCast3D InteractRaycast;
	// The target that the pickupable body is being held at.
	private Node3D PickupableBodyTarget;
	
	#endregion

	#region Regular Variables
	// The interactable that the player is currently looking at. Can be a button, a holdable object, etc.
	private Node3D _SelectedInteractable = null;
	public Node3D SelectedInteractable
	{
		get { return _SelectedInteractable; }
		private set
		{
			SetSelectedInteractable(value);
		}
	}
	// The physical body that the player is currently holding. Can be null.
	private PickupableBody _HeldBody = null;
	public PickupableBody HeldBody
	{
		get {return _HeldBody;}
		private set
		{
			SetHeldBody(value, _HeldBody);
		}
	}
	public float GlobalSpeedModifier = 1.0f;

	#endregion

	#region Exported Variables
	[Export] public bool LoggingDebug = false;
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 1.0f;
	[Export] public float SprintSpeedMultiplier = 1.5f;

	#endregion

	#region Signals
	[Signal] public delegate void SelectedInteractableChangedEventHandler(Node3D NewInteractable);

	#endregion

    public override void _Ready()
    {
		InitRefs();
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
		// Initialize locomotion state
		if (!IsOnFloor())
		{
			LocomotionStateMachine.EnterState("FallingState");
		} else {
			LocomotionStateMachine.EnterState("GroundedState");
		}
    }


	public override void _Input(InputEvent NewEvent)
	{
		if (NewEvent is InputEventMouseMotion)
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
			if (Input.IsActionJustPressed("Interact"))
			{
				Debug.Log(GetInteractableFromRaycast());

				// If target is a pickupable
				if (GetInteractableFromRaycast() is PickupableBody BodyToPickup)
				{
					// If holding nothing
					if (HeldBody is null)
					{
						// attempt to hold the new body
						TryPickupPickupableBody(BodyToPickup);
					}
					// if holding something
					else if (HeldBody is not null)
					{
						// drop the held thing
						DropHeldBody();
					}
				}
				// If target is a button
				else if (GetInteractableFromRaycast() is ButtonInteractable ButtonToInteract)
				{
					ButtonToInteract.InteractWithButton();
				}
				// If target is nothing
				else if (GetInteractableFromRaycast() is null)
				{
					// if holding something, drop the held body
					if (HeldBody is not null)
					{
						DropHeldBody();
					}
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		TrySelectInteractable();
	}

	public override void _PhysicsProcess(double delta)
	{
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
		else if (Input.IsActionPressed("Sprint"))
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

		if (CurrentLocomotionStateName == "GroundedState") {
			float GroundedSpeed = Speed * GlobalSpeedModifier;
			Vector2 inputDir = Input.GetVector("Left", "Right", "Forwards", "Backwards");
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
			if (Input.IsActionJustPressed("Jump"))
			{
				velocity.Y = JumpVelocity;
			}
		}

		if (CurrentLocomotionStateName == "SprintingState") 
		{
			float SprintSpeed = Speed * SprintSpeedMultiplier * GlobalSpeedModifier;
			Vector2 inputDir = Input.GetVector("Left", "Right", "Forwards", "Backwards");
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
			if (Input.IsActionJustPressed("Jump"))
			{
				velocity.Y = JumpVelocity;
			}
		}

		#endregion


		Velocity = velocity;
		MoveAndSlide();
	}

	private void InitRefs()
	{
		PlayerCameraPivot = GetNode<Node3D>("PlayerCameraPivot");
		PrivateReferences.Add(PlayerCameraPivot);
		PlayerCamera = PlayerCameraPivot.GetNode<Camera3D>("PlayerCamera");
		PrivateReferences.Add(PlayerCamera);
		LocomotionStateMachine = GetNode<StateMachine>("LocomotionStateMachine");
		PrivateReferences.Add(LocomotionStateMachine);
		InteractRaycast = PlayerCamera.GetNode<RayCast3D>("InteractRaycast");
		PrivateReferences.Add(InteractRaycast);
		PickupableBodyTarget = PlayerCamera.GetNode<Node3D>("PickupableBodyTarget");
		PrivateReferences.Add(PickupableBodyTarget);

		if (LoggingDebug)
		{
			bool AllValidated = true;
			foreach (Node ReferenceToValidate in PrivateReferences)
			{
				if (ReferenceToValidate == null)
				{
					AllValidated = false;
					Debug.LogError("Failed to validate object reference: null");
				}
			}
			if (AllValidated)
			{
				Debug.Log("All object references validated successfully.");
			}
		}
	}

	// Updates the currently selected interactable and notifies listeners when it changes.
	private void SetSelectedInteractable(Node3D Value)
	{
		// Prevent reselecting the same interactable.
		// No change — skip deselect/select/emit.
		if (_SelectedInteractable == Value)
		{
			return;
		}

		// Clear selection on the previous interactable, if any.
		if (_SelectedInteractable is IInteractable OldInteractable)
		{
			OldInteractable.IsSelected = false;
		}

		// Store the new selection (may be null when looking at nothing).
		_SelectedInteractable = Value;

		// Mark the new interactable as selected, if any.
		if (_SelectedInteractable is IInteractable NewInteractable)
		{
			NewInteractable.IsSelected = true;
		}

		// Notify listeners of the new selection (including null when cleared).
		EmitSignal(SignalName.SelectedInteractableChanged, _SelectedInteractable);
	}

	private void SetHeldBody(PickupableBody Value, PickupableBody PreviousHeldBody)
	{
		_HeldBody = Value;

		// If the held body is null, the body is being dropped.
		if (_HeldBody == null)
		{
			PreviousHeldBody.IsHeld = false;
			PreviousHeldBody.CanBeSelected = true;
			PreviousHeldBody.HoldTarget = null;
			PreviousHeldBody.HoldingPlayer = null;
			GlobalSpeedModifier = 1.0f;
			
			return;
		}
		
		// Otherwise, the body is being picked up.
		_HeldBody.CanBeSelected = false;
		_HeldBody.IsHeld = true;
		_HeldBody.HoldingPlayer = this;
		GlobalSpeedModifier = _HeldBody.PlayerSpeedMultiplier;
	}

	// Checks for an interactable object in the player's line of sight and selects it if found.
	private void TrySelectInteractable()
	{
		Node3D Hit = GetInteractableFromRaycast();
		if (Hit is IInteractable Interactable && Interactable.CanBeSelected)
		{
			SelectedInteractable = Hit;
		}
		else
		{
			SelectedInteractable = null;
		}
	}
	
	// Returns the object the interactable checking raycast is colliding with.
	// Returns null if no object or the object does not implement IInteractable.
	private Node3D GetInteractableFromRaycast()
	{
		if (InteractRaycast == null || !InteractRaycast.IsColliding())
		{
			return null;
		}

		if (!(InteractRaycast.GetCollider() is IInteractable))
		{
			return null;
		}

		return InteractRaycast.GetCollider() as Node3D;
	}

	// Attempts to pick up the given pickupable body.
	// Returns immediately if the player is already holding a body, the body is null,
	// or the body is farther than its max hold distance.
	public void TryPickupPickupableBody(PickupableBody BodyToPickup)
	{
		// If the player is already holding a body, return.
		if (HeldBody != null)
		{
			return;
		}
		// If there is no body to pick up, return.
		if (BodyToPickup == null)
		{
			return;
		}
		// If the body is farther than its max hold distance, return.
		if (PickupableBodyTarget.GlobalPosition.DistanceTo(BodyToPickup.GlobalPosition) > BodyToPickup.MaxHoldDistance)
		{
			return;
		}

		// Set the HeldBody to it and the body's HoldTarget to the player's hold marker.
		HeldBody = BodyToPickup;
		HeldBody.HoldTarget = PickupableBodyTarget;
	}

	public void DropHeldBody()
	{
		// if held body is already null, return.
		if (HeldBody == null)
		{
			return;
		}

		// otherwise, set the held body to null.
		HeldBody = null;
	}
}
