using Godot;
using System;
using Godot.Collections;

public partial class ButtonInteractable : RigidBody3D, IInteractable
{
	[Export] public string InteractableDisplayName { get; set; }
	private bool _IsSelected;
	[Export] public bool IsSelected
	{
		get {return _IsSelected;}
		set {SetIsSelected(value);}
	}
	[Export] public Array<MeshInstance3D> InvertedHullMeshes { get; set; } = new Array<MeshInstance3D>();
	private bool _OutlineVisible = false;
	[Export] public bool OutlineVisible
	{
		get {return _OutlineVisible;}
		set {SetOutlineVisible(value);}
	}
	[Export] public bool CanBeSelected { get; set; } = true;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetIsSelected(bool Value)
	{
		_IsSelected = Value;
		OutlineVisible = _IsSelected;
	}

	public void SetOutlineVisible(bool Value)
	{
		for (int i = 0; i < InvertedHullMeshes.Count; i++)
		{
			MeshInstance3D InvertedHullMesh = InvertedHullMeshes[i];
			if (InvertedHullMesh is not null)
			{
				InvertedHullMesh.Visible = Value;
			}
		}
	}

    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
    }

}
