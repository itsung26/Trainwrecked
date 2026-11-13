using System;
using Godot;
using Godot.Collections;

public partial class PlayerHud : Control
{
	[Export] private TextureRect DotCrosshair;
	[Export] private TextureRect SprintCrosshair;
	[Export] private Label InteractableNameLabel;
	[Export] public Label InteractableDescriptionLabel { get; set; }

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		if (!IsMultiplayerAuthority())
		{
			Visible = false;
			return;
		}

		SetInteractableNameLabelText("");
		SetInteractableDescriptionLabelText("");
	}

	public void SetInteractableNameLabelText(string text)
	{
		// Null? -> ""
        text ??= "";

		if (text == "")
		{
			InteractableNameLabel.Text = "";
		}
		else
		{
			InteractableNameLabel.Text = $"'{text}'";
		}
	}

	
	public void SetInteractableDescriptionLabelText(string text)
	{
		// Null? -> ""
		text ??= "";

		if (text == "")
		{
			InteractableDescriptionLabel.Text = "";
		}
		else
		{
			InteractableDescriptionLabel.Text = $"{text}";
		}
	}

	private void _on_locomotion_state_machine_state_changed(State NewState, State OldState)
	{
		if (!IsMultiplayerAuthority())
		{
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

	private void _on_player_selection_changed(Interactable previous, Interactable current)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		string currentName = current == null ? "" : current.DisplayName;
		string currentDesc = current == null ? "" : current.DisplayDescription;

		SetInteractableNameLabelText(currentName);
		SetInteractableDescriptionLabelText(currentDesc);
	}


}
