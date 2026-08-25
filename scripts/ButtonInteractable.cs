using Godot;
using System;
using Godot.Collections;

[GlobalClass]
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
	[Export] public bool CanBeInteractedWith { get; set; } = true;
	[Export] public AnimationPlayer ButtonAnimator { get; set; } = new AnimationPlayer();
	[Export] public String InteractAnimationName { get; set; }


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Debug.Log(CanBeInteractedWith);
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

	// Called when the button is interacted with.
	// When overriding, call the base method.
	public virtual void InteractWithButton()
	{
		if (!CanBeInteractedWith)
		{
			return;
		}
		else if (CanBeInteractedWith)
		{
			CanBeInteractedWith = false;
			ButtonAnimator.Play(InteractAnimationName);
			ButtonAnimator.Connect(
				AnimationPlayer.SignalName.AnimationFinished,
				Callable.From<StringName>(OnInteractAnimationFinished),
				(uint)ConnectFlags.OneShot
			);
		}
	}

	// Called when the interact animation finishes playing.
	// When overriding, call the base method.
	public virtual void OnInteractAnimationFinished(StringName AnimName)
	{
		CanBeInteractedWith = true;
	}

    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
    }

}
