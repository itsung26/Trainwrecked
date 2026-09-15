using Godot;
using System;
using System.ComponentModel;

public partial class TrainwreckedMainMenu : Control
{
	[Export] public MultiplayerLobbyMenu MultiplayerLobbyMenu { get; set; }
	[Export] public Button ContinueButton { get; set; }
	[Export] public Button MultiplayerButton { get; set; }
	[Export] public Button NewGameButton { get; set; }
	[Export] public Button LoadButton { get; set; }
	[Export] public Button OptionsButton { get; set; }
	[Export] public Button QuitButton { get; set; }
	[Export] public Vector3 CameraInitialPosition { get; set; }
	[Export] public Vector3 CameraInitialRotation { get; set; }
	[Export] public StateMachine MainMenuStateMachine { get; set; }
	[Export] public Control MainMenuScreenAssembly { get; set; }


	public override void _Ready()
	{
		MainMenuStateMachine.EnterState("TitleScreenState");
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
		NetworkManager.Instance.ConnectionFailed += _on_connection_failed;
		NetworkManager.Instance.ServerDisconnected += _on_server_disconnected;
		UpdateButtonRestrictions();
	}

	public void UpdateButtonRestrictions()
	{
		bool isClient = NetworkManager.IsClient();

		NewGameButton.Disabled = isClient;
		LoadButton.Disabled = isClient;
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
		LevelLoader.LoadMainLevel();
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

	private void _on_multiplayer_lobby_menu_exited_menu()
	{
		UpdateButtonRestrictions();
	}

	private void _on_connected_to_server()
	{
		UpdateButtonRestrictions();
	}

	private void _on_connection_failed()
	{
		UpdateButtonRestrictions();
	}

	private void _on_server_disconnected()
	{
		UpdateButtonRestrictions();
	}

}
