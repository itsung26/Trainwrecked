using Godot;
using System;

public partial class PlayerHud : Control
{
	[Export] public TextureRect DotCrosshair;
	[Export] public TextureRect SprintCrosshair;
	[ExportCategory("Debug Text Box")]
	[Export] public Label CurrentSelectedLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
}
