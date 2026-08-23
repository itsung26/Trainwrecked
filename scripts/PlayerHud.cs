using Godot;
using System;
using Godot.Collections;

public partial class PlayerHud : Control
{
	[Export] public TextureRect DotCrosshair;
	[Export] public TextureRect SprintCrosshair;
	[Export] public Label CurrentSelectedLabel;
	[Export] public Label InteractableNameLabel;
	[Export] public Label InteractKeybindVisualLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InteractableNameLabel.Visible = false;
		InteractKeybindVisualLabel.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetInteractableName(string Name)
	{
		InteractableNameLabel.Text = Name;
	}

	public void _on_locomotion_state_machine_state_changed(State NewState, State OldState)
	{
		if (NewState.Name == "SprintingState")
		{
			SprintCrosshair.Visible = true;
			DotCrosshair.Visible = false;
		}
		else if (NewState.Name == "GroundedState")
		{
			DotCrosshair.Visible = true;
			SprintCrosshair.Visible = false;
		}
	}

	public void _on_player_selected_interactable_changed(Node3D NewInteractable)
	{
		if (NewInteractable is IInteractable Interactable)
		{
			InteractableNameLabel.Visible = true;
			InteractKeybindVisualLabel.Visible = true;
			string NameToDisplay = Interactable.InteractableDisplayName;
			InteractableNameLabel.Text = "'" + NameToDisplay + "'";
		}
		else if (NewInteractable == null)
		{
			InteractableNameLabel.Visible = false;
			InteractKeybindVisualLabel.Visible = false;
			InteractableNameLabel.Text = "''";
		}
	}

}
