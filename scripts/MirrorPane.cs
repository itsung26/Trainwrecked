using Godot;
using System;

[Tool]
public partial class MirrorPane : Node3D
{
	private Vector2 _size = new(1.0f, 1.0f);
	[Export] public Vector2 Size
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

	private void SetSize(Vector2 value) {
		_size = value;
		if (_quadMesh?.Mesh is QuadMesh meshResource)
			meshResource.Size = _size;
	}
}
