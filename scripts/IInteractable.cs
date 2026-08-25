using Godot;
using System;
using Godot.Collections;

public interface IInteractable
{
    // These properties must be exported from any class implementing this interface.
    // Exporting these properties from the interface itself has no effect.

    string InteractableDisplayName { get; set; }
    bool IsSelected { get; set; }
	Array<MeshInstance3D> InvertedHullMeshes { get; set; }
	bool OutlineVisible { get; set; }
    bool CanBeSelected { get; set; }
}