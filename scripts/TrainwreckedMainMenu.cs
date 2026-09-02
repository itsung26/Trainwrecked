using Godot;
using System;

public partial class TrainwreckedMainMenu : Control
{
	private MultiplayerLobbyMenu MultiplayerLobbyMenu;

    public override void _Ready()
    {
        InitRefs();
    }

	private void InitRefs()
	{
		MultiplayerLobbyMenu = GetNode<MultiplayerLobbyMenu>("MultiplayerLobbyMenu");
	}

	private void _on_continue_button_pressed()
	{
	}

	private void _on_multiplayer_button_pressed()
	{
		MultiplayerLobbyMenu.Visible = true;
	}

	private void _on_new_game_button_pressed()
	{
	}

	private void _on_load_button_pressed()
	{
	}

	private void _on_options_button_pressed()
	{
	}

	private void _on_quit_button_pressed()
	{
		GetTree().Quit(); // placeholder
	}
}
