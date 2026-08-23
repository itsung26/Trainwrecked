using Godot;
using System;

public partial class Player : CharacterBody3D
{
	#region Private References
	private Node3D PlayerCameraPivot;
	private Camera3D PlayerCamera;
	private StateMachine LocomotionStateMachine;
	
	#endregion

	#region Regular Variables
	// The interactable that the player is currently looking at. Can be a button, a holdable object, etc.
	public Node3D SelectedInteractable = null;
	// The physical body that the player is currently holding. Can be null.
	public PickupableBody HeldBody {get; private set;} = null;

	#endregion

	#region Exported Variables
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 1.0f;
	[Export] public float SprintSpeedMultiplier = 1.5f;

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
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		String CurrentLocomotionStateName = LocomotionStateMachine.CurrentState.Name;

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
			Vector2 inputDir = Input.GetVector("Left", "Right", "Forwards", "Backwards");
			Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			if (direction != Vector3.Zero)
			{
				velocity.X = direction.X * Speed;
				velocity.Z = direction.Z * Speed;
			}
			else
			{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
				velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
			}
			
			// Handle jump AFTER lateral movement to avoid b-hopping.
			if (Input.IsActionJustPressed("Jump"))
			{
				velocity.Y = JumpVelocity;
			}
		}

		if (CurrentLocomotionStateName == "SprintingState") 
		{
			Vector2 inputDir = Input.GetVector("Left", "Right", "Forwards", "Backwards");
			Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
			if (direction != Vector3.Zero)
			{
				velocity.X = direction.X * (Speed * SprintSpeedMultiplier);
				velocity.Z = direction.Z * (Speed * SprintSpeedMultiplier);
			}
			else
			{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, (Speed * SprintSpeedMultiplier));
				velocity.Z = Mathf.MoveToward(Velocity.Z, 0, (Speed * SprintSpeedMultiplier));
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
		PlayerCamera = PlayerCameraPivot.GetNode<Camera3D>("PlayerCamera");
		LocomotionStateMachine = GetNode<StateMachine>("LocomotionStateMachine");
	}
}
