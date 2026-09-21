using Godot;
using Godot.Collections;
using System;
using System.Linq;

/// <summary>
/// Abstract base class for world objects that can be highlighted when looked at (becomes selected)
/// and activated when the player presses Interact. Players will not see interactables highlight when other players select them.
/// </summary>
/// <remarks>
/// Selection outline VFX uses descendant <see cref="MeshInstance3D"/> nodes whose
/// name contains <c>IH</c> (inverted hull). Subclasses such as
/// <see cref="PickupableBody"/> and <see cref="ButtonInteractable"/> add interaction
/// behavior; outline visibility is shared here via <see cref="OutlineVisible"/>.
/// </remarks>
[GlobalClass]
public abstract partial class Interactable : RigidBody3D
{
	private Array<MeshInstance3D> _invertedHullMeshes = new Array<MeshInstance3D>();
	private bool _outlineVisible = false;
	private bool _selectable = true;

	/// <summary>
	/// Whether the inverted-hull outline meshes are shown.
	/// Setting this updates every cached IH <see cref="MeshInstance3D"/>'s
	/// <see cref="Node3D.Visible"/> flag.
	/// </summary>
	public bool OutlineVisible
	{
		get { return _outlineVisible; }
		set { SetOutlineVisible(value); }
	}

	public bool Selectable
	{
		get { return _selectable; }
		set { SetSelectable(value); }
	}

	/// <summary>
	/// Logs an error when no inverted-hull meshes were cached, then hides the outline.
	/// </summary>
	public override void _Ready()
	{
		PopulateInvertedHullMeshes();
		if (_invertedHullMeshes.Count == 0)
		{
			Debug.LogWarn("No inverted hull meshes were detected. Add them by adding \"IH\" to their name.");
		}
		
		OutlineVisible = false;
	}

	/// <summary>
	/// Finds descendant <see cref="MeshInstance3D"/> nodes whose name contains
	/// <c>IH</c> and stores them in <c>_invertedHullMeshes</c>.
	/// </summary>
	private void PopulateInvertedHullMeshes()
	{
		Array<MeshInstance3D> meshes = CollectDescendants<MeshInstance3D>();
		foreach (MeshInstance3D mesh in meshes)
		{
			if (mesh.Name.ToString().Contains("IH"))
			{
				_invertedHullMeshes.Add(mesh);
			}
		}
	}

	/// <summary>
	/// Applies outline visibility to all cached inverted-hull meshes.
	/// </summary>
	/// <param name="value">
	/// <see langword="true"/> to show the outline; <see langword="false"/> to hide it.
	/// </param>
	private void SetOutlineVisible(bool value)
	{
		_outlineVisible = value;
		foreach (MeshInstance3D mesh in _invertedHullMeshes)
		{
			if (mesh != null)
			{
				mesh.Visible = value;
			}
		}
	}

	/// <summary>
	/// Returns all descendant nodes of this node that are of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">
	/// Variant-compatible reference type to match (for example <see cref="MeshInstance3D"/>).
	/// </typeparam>
	/// <returns>A Godot array of matching descendants; empty if none match.</returns>
	private Array<T> CollectDescendants<[MustBeVariant] T>() where T : class
	{
		Array<T> descendants = new Array<T>();

		void Collect(Node root)
		{
			foreach (Node child in root.GetChildren())
			{
				if (child is T typed)
				{
					descendants.Add(typed);
				}
				Collect(child);
			}
		}

		Collect(this);
		return descendants;
	}

	private void SetSelectable(bool value)
	{
		_selectable = value;
	}

}
