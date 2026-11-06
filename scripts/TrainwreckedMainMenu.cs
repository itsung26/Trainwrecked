using System;
using Godot;

public partial class TrainwreckedMainMenu : Control
{
	[Export] public float ContinueLabelPulseFrequency { get; set; } = 1.0f;
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
	[Export] public Label ContinueLabel { get; set; }

	public override void _Ready()
	{
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
		NetworkManager.Instance.ConnectionFailed += _on_connection_failed;
		NetworkManager.Instance.ServerDisconnected += _on_server_disconnected;
		UpdateButtonRestrictions();
		GetTree().CreateTimer(0.5f).Timeout += () => SkipIntro();
	}

	public override void _Input(InputEvent @event)
	{
		if (StateMachine.CurrentState.Name == "AwaitingInput"
			&& (@event is InputEventMouseButton
				|| (@event is InputEventKey keyEvent
					&& keyEvent.PhysicalKeycode is not (Key.Meta or Key.Alt or Key.Tab))))
		{
			StateMachine.EnterState("FinalTween");
		}
		// else if (StateMachine.CurrentState.Name != "MainMenuDone" && SkipIntroAllowed)
		// {
		// 	if (@event is InputEventMouseButton || (@event is InputEventKey eventkey && eventkey.PhysicalKeycode is not (Key.Meta or Key.Alt or Key.Tab)))
		// 	{
		// 		MenuIntroAnimator.SpeedScale = 5.0f;
		// 		StateMachine.EnterState("FinalTween");
		// 	}
		// }
	}

    public override void _Process(double delta)
    {
        if (StateMachine.CurrentState.Name == "AwaitingInput")
		{
			ContinueLabel.Modulate = LerpColor(
				Colors.White, 
				Colors.Transparent, 
				0.5f * Mathf.Sin(Time.GetTicksMsec() / 1000f * ContinueLabelPulseFrequency) + 0.5f
				);
		}
    }

	public void UpdateButtonRestrictions()
	{
		bool isClient = NetworkManager.IsClient();

		NewGameButton.Disabled = isClient;
		LoadButton.Disabled = isClient;
	}

	/// Returns a new color interpolated between a and b by weight.
	private static Color LerpColor(Color a, Color b, float weight)
	{
		float red = Mathf.Lerp(a.R, b.R, weight);
		float green = Mathf.Lerp(a.G, b.G, weight);
		float blue = Mathf.Lerp(a.B, b.B, weight);
		float alpha = Mathf.Lerp(a.A, b.A, weight);

		return new Color(red, green, blue, alpha);
	}

	private void SkipIntro()
	{
		if (StateMachine.CurrentState.Name == "AwaitingInput" ||
		StateMachine.CurrentState.Name == "MainMenuDone")
		{
			return;
		}

		MenuIntroAnimator.SpeedScale = 5.0f;
		MenuIntroAnimator.AnimationFinished += animName =>
        {
			if (animName == "FinalTweenAnim")
			{
				MenuIntroAnimator.SpeedScale = 5.0f;
			}
		};
		StateMachine.EnterState("FinalTween");
	}

	#region Event Handlers
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
		GetTree().Quit();
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
				MenuIntroAnimator.Play("InitialTweenAnim");
				break;

			case "AwaitingInput":
				break;

			case "FinalTween":
				MenuIntroAnimator.Play("FinalTweenAnim");
				break;
			case "MainMenuDone":
				break;
		}
	}
	#endregion
}
