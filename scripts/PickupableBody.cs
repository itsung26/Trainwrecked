using Godot;
using System;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class PickupableBody : RigidBody3D
{
	// When true, the InvertedHullMeshes will be visible.
	private bool _OutlineVisible = false;
	[Export]
	public bool OutlineVisible
	{
		get { return _OutlineVisible; }
		set
		{
			_OutlineVisible = value;
			UpdateOutlineVisibility();
		}
	}
	[Export] public Array<MeshInstance3D> InvertedHullMeshes = new Array<MeshInstance3D>();
	public bool IsHeld = false;


    public override void _Ready()
    {
        OutlineVisible = false;
    }

	private void UpdateOutlineVisibility()
	{
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
