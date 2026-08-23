using Godot;
using System;
using Godot.Collections;

public interface IInteractable
{
    string InteractableDisplayName { get; set; }
    bool IsSelected { get; set; }
	Array<MeshInstance3D> InvertedHullMeshes { get; set; }
	bool OutlineVisible { get; set; }
    bool CanBeSelected { get; set; }
}