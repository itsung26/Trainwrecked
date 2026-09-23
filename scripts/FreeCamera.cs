using Godot;
using System;
using System.ComponentModel;

/// <summary>
/// Not intended for use in multiplayer. Debug only.
/// </summary>
public partial class FreeCamera : Camera3D
{
	public enum Axes
	{
		NegativeZ,
		PositiveZ,
		NegativeY,
		PositiveY,
		NegativeX,
		PositiveX
	}

	/// <summary>
	/// When true, Rotation is locked to keep the camera looking TOWARDS that axis.
	/// For example, a lock to NegativeZ will keep the camera looking away from z and towards z-.
	/// </summary>
	[Export] public bool LockRotation { get; set; }
	[Export] public Axes RotationLockAxis { get; set; }
	[Export] public float LookSensitivity { get; set; } = 1.0f;
	[Export] public float MoveSpeed { get; set; } = 10.0f;
	[Export] public float SprintSpeedMultiplier { get; set; } = 2.5f;

	public override void _Input(InputEvent @event)
	{
		if (LockRotation || @event is not InputEventMouseMotion motion)
		{
			return;
		}

		float yaw = Rotation.Y - motion.Relative.X * LookSensitivity / 10.0f;
		float pitch = Math.Clamp(
			Rotation.X - motion.Relative.Y * LookSensitivity / 10.0f,
			-1.5f,
			1.5f
		);
		Rotation = new Vector3(pitch, yaw, 0.0f);
	}

	public override void _Ready()
	{
		if (NetworkManager.IsConnected())
		{
			throw new NotImplementedException("Freecam not intended for use in multiplayer.");
		}

		Input.SetMouseMode(Input.MouseModeEnum.Captured);
	}

	public override void _Process(double delta)
	{
		UpdateCameraLockedRotation();
		UpdateCameraMovement((float)delta);
	}

	/// <summary>
	/// Fly the camera with WASD, Jump/Down for vertical, Sprint for a speed boost.
	/// Movement is relative to the camera's facing direction.
	/// </summary>
	private void UpdateCameraMovement(float delta)
	{
		Vector2 planar = Input.GetVector("Left", "Right", "Forwards", "Backwards");
		float vertical = 0.0f;
		if (Input.IsActionPressed("Jump"))
		{
			vertical += 1.0f;
		}
		if (Input.IsActionPressed("Down"))
		{
			vertical -= 1.0f;
		}

		if (planar == Vector2.Zero && vertical == 0.0f)
		{
			return;
		}

		Basis basis = GlobalTransform.Basis;
		Vector3 direction = (-basis.Z * planar.Y) + (basis.X * planar.X) + (Vector3.Up * vertical);
		if (direction.LengthSquared() > 0.0f)
		{
			direction = direction.Normalized();
		}

		float speed = MoveSpeed;
		if (Input.IsActionPressed("Sprint"))
		{
			speed *= SprintSpeedMultiplier;
		}

		GlobalPosition += direction * speed * delta;
	}

	/// <summary>
	/// Keep the camera rotation locked to the chosen axis.
	/// </summary>
	private void UpdateCameraLockedRotation()
	{
		if (!LockRotation)
		{
			return;
		}

		switch (RotationLockAxis)
		{
			case Axes.NegativeZ:
				GlobalRotationDegrees = Vector3.Zero;
				break;

			case Axes.PositiveZ:
				GlobalRotationDegrees = new Vector3(0.0f, 180.0f, 0.0f);
				break;

			case Axes.NegativeY:
				GlobalRotationDegrees = new Vector3(-90.0f, 0.0f, 0.0f);
				break;

			case Axes.PositiveY:
				GlobalRotationDegrees = new Vector3(90.0f, 0.0f, 0.0f);
				break;

			case Axes.NegativeX:
				GlobalRotationDegrees = new Vector3(0.0f, 90.0f, 0.0f);
				break;

			case Axes.PositiveX:
				GlobalRotationDegrees = new Vector3(0.0f, -90.0f, 0.0f);
				break;
		}
	}

}
