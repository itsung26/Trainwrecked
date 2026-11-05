using Godot;

public partial class TrainwreckedMainMenu : Control
{
	[ExportGroup("Nodes")]
	[Export] public MultiplayerLobbyMenu MultiplayerLobbyMenu { get; set; }
	[Export] public Button ContinueButton { get; set; }
	[Export] public Button MultiplayerButton { get; set; }
	[Export] public Button NewGameButton { get; set; }
	[Export] public Button LoadButton { get; set; }
	[Export] public Button OptionsButton { get; set; }
	[Export] public Button QuitButton { get; set; }
	[Export] public AnimationPlayer MenuIntroAnimator { get; set; }
	[Export] public StateMachine StateMachine { get; set; }

	public override void _Ready()
	{
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
		NetworkManager.Instance.ConnectionFailed += _on_connection_failed;
		NetworkManager.Instance.ServerDisconnected += _on_server_disconnected;
		UpdateButtonRestrictions();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey)
		{
			if (StateMachine.CurrentState.Name == "AwaitingInput")
			{
				StateMachine.EnterState("FinalTween");
			}
		}
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

	private void _on_state_machine_state_changed(State newState, State oldState)
	{
		switch (newState.Name)
		{

			case "InitialTween":
				MenuIntroAnimator.Play("intro");
				break;

			case "AwaitingInput":
				break;

			case "FinalTween":
				break;

		}
	}
}
