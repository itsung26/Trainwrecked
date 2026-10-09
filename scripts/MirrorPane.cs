using System;
using Godot;

/// <summary>
/// Reflective pane. <see cref="MirrorCamera"/> is the <see cref="ObserverCamera"/>
/// reflected across this node's local XY plane, and its view is drawn on <c>MirrorMesh</c>.
/// </summary>
/// <remarks>
/// Requires unique child nodes <c>MirrorMesh</c>, <c>MirrorViewport</c>, and <c>MirrorCamera</c>.
/// Runs in the editor via <see cref="ToolAttribute"/>.
/// </remarks>
[Tool]
public partial class MirrorPane : Node3D
{
	/// <summary>Which mesh <c>MirrorMesh</c> displays.</summary>
	public enum MeshModes
	{
		/// <summary>A generated <see cref="QuadMesh"/> sized by <see cref="Size"/>.</summary>
		Quad,
		/// <summary>The mesh assigned to <see cref="CustomMesh"/>.</summary>
		Custom
	}

	/// <summary>Camera reflected into <c>MirrorCamera</c>. No reflection is updated when this is <see langword="null"/>.</summary>
	[Export] public Camera3D ObserverCamera { get; set; }
	private MeshModes _meshMode = MeshModes.Quad;
	/// <summary>
	/// Selects the mesh on <c>MirrorMesh</c> and applies it immediately.
	/// </summary>
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
	/// <summary>
	/// Width and height of the generated quad, in this node's local X and Y.
	/// The value is always stored. The quad is rebuilt only while <see cref="MeshMode"/> is <see cref="MeshModes.Quad"/>.
	/// </summary>
	[Export]
	public Vector2 Size
	{
		get => _size;
		set => SetSize(value);
	}
	private ArrayMesh _customMesh;
	/// <summary>
	/// Mesh shown while <see cref="MeshMode"/> is <see cref="MeshModes.Custom"/>.
	/// Assigning it reapplies the mesh only in that mode.
	/// </summary>
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

	/// <summary>Caches the mirror nodes and applies <see cref="MeshMode"/>.</summary>
	public override void _Ready()
	{
		_mirrorMesh = GetNode<MeshInstance3D>("%MirrorMesh");
		_mirrorViewport = GetNode<SubViewport>("%MirrorViewport");
		_mirrorCamera = GetNode<Camera3D>("%MirrorCamera");

		ApplyMesh();
	}

	/// <summary>
	/// Places <c>MirrorCamera</c> at the reflected observer pose and clips its near plane toward the glass.
	/// </summary>
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

	/// <summary>
	/// Assigns either a new flipped <see cref="QuadMesh"/> of <see cref="Size"/>,
	/// or <see cref="CustomMesh"/>, to <c>MirrorMesh</c>.
	/// </summary>
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

	/// <summary>
	/// Returns <paramref name="observerCameraWorldPos"/> reflected across this pane's local XY plane.
	/// </summary>
	private Vector3 CalculateMirrorCameraPosition(Vector3 observerCameraWorldPos)
	{
		Vector3 observerCameraLocalPos = ToLocal(observerCameraWorldPos);
		return ToGlobal(new Vector3(observerCameraLocalPos.X, observerCameraLocalPos.Y, -observerCameraLocalPos.Z));
	}

	/// <summary>
	/// Sets <c>MirrorCamera.Near</c> to the distance along its forward axis to this pane's local Z = 0 plane.
	/// </summary>
	/// <remarks>
	/// The near plane stays perpendicular to the view. It meets the glass along the center ray only,
	/// so a glancing <see cref="ObserverCamera"/> still leaves a wedge of the space behind the glass visible.
	/// Does nothing when the camera is looking along the glass or away from it.
	/// </remarks>
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

	/// <summary>
	/// Returns <see cref="ObserverCamera"/>'s basis reflected across this pane's local XY plane.
	/// Each local axis keeps its X and Y and negates Z.
	/// </summary>
	private Basis CalculateMirrorCameraRotation()
	{
		Basis local = GlobalTransform.Basis.Inverse() * ObserverCamera.GlobalTransform.Basis;
		local.X = new Vector3(local.X.X, local.X.Y, -local.X.Z);
		local.Y = new Vector3(local.Y.X, local.Y.Y, -local.Y.Z);
		local.Z = new Vector3(local.Z.X, local.Z.Y, -local.Z.Z);
		return GlobalTransform.Basis * local;
	}
}
