using Godot;
using System;
using Godot.Collections;

public partial class ButtonInteractable : RigidBody3D, IInteractable
{
	[Export] public String InteractableDisplayName;
	[Export] public bool IsSelected;
	[Export] public Array<MeshInstance3D> InvertedHullMeshes;
	[Export] public bool OutlineVisible;
	[Export] public bool CanBeSelected;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
