using Godot;
using System;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class PickupableBody : RigidBody3D, IInteractable
{
	#region Regular Variables
	public bool IsHeld {get; private set;} = false;
	private bool _IsSelected = false;
	public bool IsSelected
	{
		get { return _IsSelected; }
		set
		{
			_IsSelected = value;
			OutlineVisible = _IsSelected;
		}
	}
	public bool CanBeSelected {get; set;} = true;

	#endregion

	#region Exported Variables
	// When true, the InvertedHullMeshes will be visible.
	private bool _OutlineVisible = false;
	[Export]
	public bool OutlineVisible
	{
		get { return _OutlineVisible; }
		set
		{
			_OutlineVisible = value;

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
	// The meshes that consist of the inverted hull outline of the body.
	[Export] public Array<MeshInstance3D> InvertedHullMeshes { get; set; } = new Array<MeshInstance3D>();
	// The name of the body that will be displayed when it is selected.
	[Export] public string InteractableDisplayName {get; set;}
	// How tightly the body will follow the player's hand when picked up.
	[Export] public float HoldSpring = 0.5f;
	// The maximum distance the body will be allowed to be from the player's hand when picked up.
	[Export] public float MaxHoldDistance = 0.5f;
	// How much the player's speed will be scaled by when holding a body.
	[Export] public float PlayerSpeedMultiplier = 1.0f;

	#endregion


	public override void _Ready()
	{
		OutlineVisible = false;
	}

	public override string ToString()
	{
		return Name + "#" + GetInstanceId();
	}

}
