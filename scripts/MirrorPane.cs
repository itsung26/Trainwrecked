using System;
using Godot;

[Tool]
public partial class MirrorPane : Node3D
{
	public enum MeshModes
	{
		Quad,
		Custom
	}

	[Export] public Camera3D ObserverCamera { get; set; }
	private MeshModes _meshMode = MeshModes.Quad;
	[Export]
	public MeshModes MeshMode
	{
		get => _meshMode;
		set
		{
			_meshMode = value;
			ApplyMesh();
		}
	}
	private Vector2 _size = new(1.0f, 1.0f);
	[Export]
	public Vector2 Size
	{
		get => _size;
		set => SetSize(value);
	}
	private ArrayMesh _customMesh;
	[Export]
	public ArrayMesh CustomMesh
	{
		get => _customMesh;
		set
		{
			_customMesh = value;
			if (_meshMode == MeshModes.Custom)
			{
				ApplyMesh();
			}
		}
	}
	private QuadMesh _quadMesh;
	private MeshInstance3D _mirrorMesh;
	private SubViewport _mirrorViewport;
	private Camera3D _mirrorCamera;

	public override void _Ready()
	{
		_mirrorMesh = GetNode<MeshInstance3D>("%MirrorMesh");
		_mirrorViewport = GetNode<SubViewport>("%MirrorViewport");
		_mirrorCamera = GetNode<Camera3D>("%MirrorCamera");

		ApplyMesh();
	}

	public override void _Process(double delta)
	{
		if (ObserverCamera is not null)
		{
			_mirrorCamera.GlobalPosition = CalculateMirrorCameraPosition(ObserverCamera.GlobalPosition);
			_mirrorCamera.GlobalBasis = CalculateMirrorCameraRotation();
			ClipNearPlaneToMirror();
		}
	}

	private void SetSize(Vector2 value)
	{
		_size = value;
		if (_meshMode == MeshModes.Quad)
		{
			ApplyMesh();
		}
	}

	private void ApplyMesh()
	{
		if (_mirrorMesh is null)
		{
			return;
		}

		if (_meshMode == MeshModes.Custom)
		{
			_mirrorMesh.Mesh = _customMesh;
			return;
		}

		_quadMesh = new QuadMesh();
		_quadMesh.FlipFaces = true;
		_quadMesh.Size = _size;
		_mirrorMesh.Mesh = _quadMesh;
	}

	// Returns the mirrored position of the observer camera in world coordinates.

	private Vector3 CalculateMirrorCameraPosition(Vector3 observerCameraWorldPos)
	{
		Vector3 observerCameraLocalPos = ToLocal(observerCameraWorldPos);
		return ToGlobal(new Vector3(observerCameraLocalPos.X, observerCameraLocalPos.Y, -observerCameraLocalPos.Z));
	}

	// Places the near plane on the glass so the back of the frame is not rendered.
	private void ClipNearPlaneToMirror()
	{
		Vector3 localPosition = ToLocal(_mirrorCamera.GlobalPosition);
		Basis localBasis = GlobalTransform.Basis.Inverse() * _mirrorCamera.GlobalBasis;
		float forwardZ = -localBasis.Z.Z;
		if (Mathf.Abs(forwardZ) < 0.001f)
		{
			return;
		}

		float distance = -localPosition.Z / forwardZ;
		if (distance <= 0.0f)
		{
			return;
		}

		_mirrorCamera.Near = Mathf.Min(distance, _mirrorCamera.Far * 0.99f);
	}

	// Returns the observer basis reflected across this pane's local XY plane.

	private Basis CalculateMirrorCameraRotation()
	{
		Basis local = GlobalTransform.Basis.Inverse() * ObserverCamera.GlobalTransform.Basis;
		local.X = new Vector3(local.X.X, local.X.Y, -local.X.Z);
		local.Y = new Vector3(local.Y.X, local.Y.Y, -local.Y.Z);
		local.Z = new Vector3(local.Z.X, local.Z.Y, -local.Z.Z);
		return GlobalTransform.Basis * local;
	}
}
