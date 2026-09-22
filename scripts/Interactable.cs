using Godot;
using Godot.Collections;
using System;
using System.Linq;

/// <summary>
/// Abstract base for world objects that can be highlighted when looked at and
/// activated when the player presses Interact.
/// </summary>
/// <remarks>
/// Selection and outline are local visual state: peers do not see each other's
/// highlights. Outline VFX uses descendant <see cref="MeshInstance3D"/> nodes
/// whose name contains <c>IH</c> (inverted hull). Subclasses such as
/// <see cref="PickupableBody"/> and <see cref="ButtonInteractable"/> implement
/// <see cref="Interact"/>. <see cref="Player"/> drives <see cref="Selected"/>
/// from its interact raycast.
/// </remarks>
[GlobalClass]
public abstract partial class Interactable : RigidBody3D
{
	private Array<MeshInstance3D> _invertedHullMeshes = new Array<MeshInstance3D>();
	private bool _outlineVisible = false;
	private bool _selectable = true;
	private bool _selected = false;

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

	/// <summary>
	/// Whether this interactable can become <see cref="Selected"/>.
	/// </summary>
	/// <remarks>
	/// Setting this to <see langword="false"/> clears <see cref="Selected"/>.
	/// Setting <see cref="Selected"/> to <see langword="true"/> while this is
	/// <see langword="false"/> is ignored.
	/// </remarks>
	public bool Selectable
	{
		get { return _selectable; }
		set { SetSelectable(value); }
	}

	/// <summary>
	/// Whether the local player currently has this interactable targeted.
	/// </summary>
	/// <remarks>
	/// When set to <see langword="true"/>, shows the outline via
	/// <see cref="OutlineVisible"/>; when <see langword="false"/>, hides it.
	/// Requires <see cref="Selectable"/> to be <see langword="true"/> to select.
	/// </remarks>
	public bool Selected
	{
		get { return _selected; }
		set { SetSelected(value); }
	}

	/// <summary>
	/// Caches inverted-hull meshes, warns if none were found, then hides the outline.
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

	/// <summary>
	/// Sets whether this interactable may be selected.
	/// </summary>
	/// <param name="value">
	/// <see langword="true"/> to allow selection; <see langword="false"/> to
	/// disallow it and clear <see cref="Selected"/>.
	/// </param>
	public void SetSelectable(bool value)
	{
		_selectable = value;

		if (value == false)
		{
			Selected = false;
		}
	}

	/// <summary>
	/// Sets whether this interactable is the local player's current target and
	/// syncs <see cref="OutlineVisible"/> to match.
	/// </summary>
	/// <param name="value">
	/// <see langword="true"/> to select and show the outline;
	/// <see langword="false"/> to deselect and hide it.
	/// </param>
	/// <remarks>
	/// Does nothing when selecting while <see cref="Selectable"/> is
	/// <see langword="false"/>.
	/// </remarks>
	public void SetSelected(bool value)
	{
		if (!Selectable && value == true)
		{
			return;
		}

		_selected = value;

		if (value == true)
		{
			OutlineVisible = true;
		}
		else
		{
			OutlineVisible = false;
		}
	}

	/// <summary>
	/// Performs this interactable's activation when the player presses Interact.
	/// </summary>
	/// <remarks>
	/// Subclasses define the behavior. Networking (for example an <c>[Rpc]</c> on
	/// an override) is the subclass's responsibility; this base method is not an RPC.
	/// </remarks>
	public abstract void Interact();
}
