using Godot;
using System;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class PickupableBody : RigidBody3D
{
	#region Regular Variables
	public bool IsHeld = false;

	#endregion

	#region Exported Variables
	// When true, the InvertedHullMeshes will be visible.
	private bool _OutlineVisible = false;
	[Export] public bool OutlineVisible
	{
		get { return _OutlineVisible; }
		set
		{
			_OutlineVisible = value;
			UpdateOutlineVisibility();
		}
	}
	// The meshes that consist of the inverted hull outline of the body.
	[Export] public Array<MeshInstance3D> InvertedHullMeshes = new Array<MeshInstance3D>();
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

	// Called immidiately when the player picks up the body.
	public void OnPickedUp()
	{
		
	}

	// Called immidiately when the player drops the body.
	public void OnDropped()
	{
		
	}

}
