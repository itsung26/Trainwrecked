using Godot;
using Godot.Collections;

/// <summary>
/// Abstract base for world objects that can be highlighted when looked at and
/// activated when the player presses Interact.
/// </summary>
/// <remarks>
/// <para>
/// Selection and outline are local visual state: peers do not see each other's
/// highlights. <see cref="Player"/> drives <see cref="Selected"/> from its
/// interact raycast; <see cref="PlayerHud"/> reads <see cref="DisplayName"/> and
/// <see cref="DisplayDescription"/> when selection changes.
/// </para>
/// <para>
/// Outline VFX uses descendant <see cref="MeshInstance3D"/> nodes whose name
/// contains <c>IH</c> (inverted hull). Subclasses such as
/// <see cref="PickupableBody"/>, <see cref="ButtonInteractable"/>, and
/// <see cref="CarCoupler"/> implement <see cref="Interact"/>.
/// </para>
/// <para>
/// <see cref="Selectable"/> is not replicated by default. Multiplayer code that
/// must keep it in sync (for example <see cref="PickupableBody"/> hold state)
/// should set it on each peer via RPC or add it to a
/// <see cref="MultiplayerSynchronizer"/> config.
/// </para>
/// </remarks>
[Tool]
[GlobalClass]
public abstract partial class Interactable : RigidBody3D
{
	private Array<MeshInstance3D> _invertedHullMeshes = new Array<MeshInstance3D>();
	private bool _outlineVisible = false;
	private bool _selectable = true;
	private bool _selected = false;

	/// <summary>
	/// Short label shown in the local player's HUD when this interactable is selected.
	/// </summary>
	[Export] public string DisplayName { get; set; } = "???";

	/// <summary>
	/// Optional longer text shown in the local player's HUD when this interactable is selected.
	/// </summary>
	[Export] public string DisplayDescription { get; set; } = "";

	/// <summary>
	/// Whether the inverted-hull outline meshes are shown.
	/// </summary>
	/// <remarks>
	/// Setting this updates every cached IH <see cref="MeshInstance3D"/>'s
	/// <see cref="Node3D.Visible"/> flag. Driven by <see cref="Selected"/>;
	/// peers do not share this state.
	/// </remarks>
	public bool OutlineVisible
	{
		get { return _outlineVisible; }
		set { SetOutlineVisible(value); }
	}

	/// <summary>
	/// Whether this interactable can become <see cref="Selected"/>.
	/// </summary>
	/// <remarks>
	/// When <see langword="false"/>, <see cref="Player.UpdateInteractableSelection"/>
	/// treats a raycast hit as empty space (no highlight / HUD target), this type
	/// clears <see cref="Selected"/>, and attempts to set <see cref="Selected"/> to
	/// <see langword="true"/> are ignored. Defaults to <see langword="true"/>.
	/// Not networked unless a subclass or synchronizer replicates it.
	/// </remarks>
	[Export]
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
	/// Local only; not replicated to other peers.
	/// </remarks>
	public bool Selected
	{
		get { return _selected; }
		set { SetSelected(value); }
	}

	/// <summary>
	/// Caches inverted-hull meshes, then hides the outline.
	/// </summary>
	/// <remarks>
	/// Skips all setup while <see cref="Engine.IsEditorHint"/> is
	/// <see langword="true"/>.
	/// </remarks>
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		PopulateInvertedHullMeshes();
		OutlineVisible = false;
	}

	/// <summary>
	/// Returns editor warnings when no descendant mesh is named for inverted-hull outlining.
	/// </summary>
	/// <returns>
	/// Base warnings plus an entry when no descendant <see cref="MeshInstance3D"/>
	/// name contains <c>IH</c>.
	/// </returns>
	public override string[] _GetConfigurationWarnings()
	{
		string[] baseWarnings = base._GetConfigurationWarnings() ?? System.Array.Empty<string>();
		System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>(baseWarnings);

		if (!HasInvertedHullMesh())
		{
			warnings.Add("No inverted hull meshes were detected. Add them by adding \"IH\" to their name.");
		}

		return warnings.ToArray();
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
	/// Returns whether any descendant <see cref="MeshInstance3D"/> name contains <c>IH</c>.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if at least one matching mesh exists; otherwise
	/// <see langword="false"/>.
	/// </returns>
	/// <remarks>
	/// Used by configuration warnings in the editor (where the runtime cache is not filled).
	/// </remarks>
	private bool HasInvertedHullMesh()
	{
		foreach (MeshInstance3D mesh in CollectDescendants<MeshInstance3D>())
		{
			if (mesh.Name.ToString().Contains("IH"))
			{
				return true;
			}
		}

		return false;
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
	private void SetSelectable(bool value)
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
	private void SetSelected(bool value)
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
	/// Returns a debug label for this node in the form <c>&lt;Name#InstanceId&gt;</c>.
	/// </summary>
	/// <returns>
	/// The string produced by <see cref="Debug.GenerateInstanceToString"/>.
	/// </returns>
	public override string ToString()
	{
		return Debug.GenerateInstanceToString(this);
	}

	/// <summary>
	/// Performs this interactable's activation when the player presses Interact.
	/// </summary>
	/// <remarks>
	/// Subclasses define the behavior. <see cref="Player"/> invokes this via
	/// <c>Rpc(Interactable.MethodName.Interact)</c> on the current selection or
	/// held body. Networking (for example an <c>[Rpc]</c> on an override) is the
	/// subclass's responsibility; this base method is not an RPC.
	/// </remarks>
	public abstract void Interact();
}
