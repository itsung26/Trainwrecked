using Godot;
using System;

[Tool]
public partial class MirrorPane : Node3D
{
	[Export] public Camera3D ObserverCamera { get; set; }
	private Vector2 _size = new(1.0f, 1.0f);
	[Export]
	public Vector2 Size
	{
		get => _size;
		set => SetSize(value);
	}
	private MeshInstance3D _quadMesh;
	private SubViewport _mirrorViewport;
	private Camera3D _mirrorCamera;

	public override void _Ready()
	{
		_quadMesh = GetNode<MeshInstance3D>("%QuadMesh");
		_mirrorViewport = GetNode<SubViewport>("%MirrorViewport");
		_mirrorCamera = GetNode<Camera3D>("%MirrorCamera");

		SetSize(_size);
	}

	public override void _Process(double delta)
	{
		if (ObserverCamera is not null)
		{
			_mirrorCamera.GlobalPosition = CalculateMirrorCameraPosition(ObserverCamera.GlobalPosition);
			_mirrorCamera.GlobalRotation = CalculateMirrorCameraRotation();
		}
	}

	private void SetSize(Vector2 value)
	{
		_size = value;
		if (_quadMesh?.Mesh is QuadMesh meshResource)
			meshResource.Size = _size;
	}

	// Returns the mirrored position of the observer camera in world coordinates.
	private Vector3 CalculateMirrorCameraPosition(Vector3 observerCameraWorldPos)
	{
		Vector3 observerCameraLocalPos = ToLocal(observerCameraWorldPos);
		return ToGlobal(new Vector3(observerCameraLocalPos.X, observerCameraLocalPos.Y, -observerCameraLocalPos.Z));
	}

	// Returns the global rotation looking at the mirror center in radians.
	private Vector3 CalculateMirrorCameraRotation()
	{
		Transform3D mirrorCameraTransform = _mirrorCamera.GlobalTransform.LookingAt(GlobalPosition);
		return mirrorCameraTransform.Basis.GetEuler();
	}
}
