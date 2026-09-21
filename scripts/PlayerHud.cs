using Godot;
using System;
using Godot.Collections;

public partial class PlayerHud : Control
{
	[Export] public TextureRect DotCrosshair;
	[Export] public TextureRect SprintCrosshair;
	[Export] public Label CurrentSelectedLabel;
	[Export] public Label InteractableNameLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (!IsMultiplayerAuthority())
		{
			Visible = false;
			return;
		}

		InteractableNameLabel.Visible = false;
	}

	public void SetInteractableNameText(string Name)
	{
		InteractableNameLabel.Text = Name;
	}

	public void _on_locomotion_state_machine_state_changed(State NewState, State OldState)
	{
		if (!IsMultiplayerAuthority()) {
			return;
		}
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


}
