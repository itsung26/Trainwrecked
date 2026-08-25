using Godot;
using System;
using Godot.Collections;

public partial class ButtonInteractable : RigidBody3D, IInteractable
{
	[Export] public string InteractableDisplayName { get; set; }
	[Export] public bool IsSelected { get; set; }
	[Export] public Array<MeshInstance3D> InvertedHullMeshes { get; set; } = new Array<MeshInstance3D>();
	[Export] public bool OutlineVisible { get; set; }
	[Export] public bool CanBeSelected { get; set; } = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
