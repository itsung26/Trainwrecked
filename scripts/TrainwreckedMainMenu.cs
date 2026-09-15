using Godot;
using System;
using System.ComponentModel;

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
	[Export] public Camera3D MainMenuCamera { get; set; }
	[Export] public StateMachine MainMenuStateMachine { get; set; }
	[Export] public Control MainMenuScreenAssembly { get; set; }
	[Export] public GpuParticles3D CirclingVultures { get; set; }
	[Export] public Timer DelayBeforeSplashScreenTransitionTimer { get; set; }
	[Export] public Timer DelayBeforeContinueTextTimer { get; set; }
	[Export] public ColorRect BlackoutRect { get; set; }
	[Export] public Label BigTitleLabel { get; set; }
	[Export] public Label ContinueLabel { get; set; }

	[ExportGroup("Settings")]
	[Export] public Vector3 CameraInitialPosition { get; set; }
	[Export] public Vector3 CameraInitialRotation { get; set; }
	[Export] public float CameraPositionTweenSpeed { get; set; } = 1.0f;
	[Export] public float CameraRotationTweenSpeed { get; set; } = 1.0f;
	[Export] public float DelayBeforeSplashScreenTransition { get; set; }
	[Export] public float DelayBeforeContinueText { get; set; }
	[Export] public float BlackoutRectFadeSpeed { get; set; } = 1.0f;
	[Export] public float ContinueLabelFadeSpeed { get; set; } = 1.0f;
	[Export] public float MainMenuScreenAssemblyFadeSpeed { get; set; } = 1.0f;

    public override void _Input(InputEvent @event)
    {
		string state = MainMenuStateMachine.CurrentState.Name;

		if (@event is InputEventKey || @event is InputEventMouseButton)
		{
			switch (state)
			{
				case null:
					break;

				case "SplashScreenState":
					break;

				case "SplashScreenTransitioningState":
					break;

				case "SplashScreenAwaitingState":
					MainMenuStateMachine.EnterState("TitleScreenTransitioningState");
					break;

				case "TitleScreenState":
					break;

				case "TitleScreenTransitioningState":
					break;

				case "MainMenuScreenState":
					break;

				default:
					break;
			}
		}
    }

	public override void _Ready()
	{
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
		NetworkManager.Instance.ConnectionFailed += _on_connection_failed;
		NetworkManager.Instance.ServerDisconnected += _on_server_disconnected;
		UpdateButtonRestrictions();
	}

	public override void _Process(double delta)
	{
		string state = MainMenuStateMachine.CurrentState.Name;

		switch (state)
		{
			case null:
				break;

			case "SplashScreenState":
				break;

			case "SplashScreenTransitioningState":
				Color blackoutColor = BlackoutRect.Color;
				BlackoutRect.Color = new Color(
					blackoutColor.R,
					blackoutColor.G,
					blackoutColor.B,
					Mathf.MoveToward(blackoutColor.A, 0.0f, BlackoutRectFadeSpeed * (float)delta)
				);
				if (BlackoutRect.Color.A == 0.0f && DelayBeforeContinueTextTimer.IsStopped())
				{
					DelayBeforeContinueTextTimer.Start(DelayBeforeContinueText);
				}
				break;

			case "SplashScreenAwaitingState":
				Color continueModulate = ContinueLabel.Modulate;
				ContinueLabel.Modulate = new Color(
					continueModulate.R,
					continueModulate.G,
					continueModulate.B,
					Mathf.MoveToward(continueModulate.A, 1.0f, ContinueLabelFadeSpeed * (float)delta)
				);
				break;

			case "TitleScreenState":
				break;

			case "TitleScreenTransitioningState":
				MainMenuCamera.Position = MainMenuCamera.Position.MoveToward(Vector3.Zero, CameraPositionTweenSpeed * (float)delta);
				MainMenuCamera.Rotation = MainMenuCamera.Rotation.MoveToward(Vector3.Zero, CameraRotationTweenSpeed * (float)delta);
				Color bigTitleModulate = BigTitleLabel.Modulate;
				BigTitleLabel.Modulate = new Color(
					bigTitleModulate.R,
					bigTitleModulate.G,
					bigTitleModulate.B,
					Mathf.MoveToward(bigTitleModulate.A, 0.0f, ContinueLabelFadeSpeed * (float)delta)
				);
				Color titleContinueModulate = ContinueLabel.Modulate;
				ContinueLabel.Modulate = new Color(
					titleContinueModulate.R,
					titleContinueModulate.G,
					titleContinueModulate.B,
					Mathf.MoveToward(titleContinueModulate.A, 0.0f, ContinueLabelFadeSpeed * (float)delta)
				);
				if (MainMenuCamera.Position == Vector3.Zero
					&& MainMenuCamera.Rotation == Vector3.Zero)
				{
					MainMenuStateMachine.EnterState("MainMenuScreenState");
				}
				break;

			case "MainMenuScreenState":
				Color assemblyModulate = MainMenuScreenAssembly.Modulate;
				MainMenuScreenAssembly.Modulate = new Color(
					assemblyModulate.R,
					assemblyModulate.G,
					assemblyModulate.B,
					Mathf.MoveToward(assemblyModulate.A, 1.0f, MainMenuScreenAssemblyFadeSpeed * (float)delta)
				);
				break;

			default:
				break;
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

	private void _on_main_menu_state_machine_state_changed(State newState, State oldState)
	{
		string newStateName = newState?.Name;
		string oldStateName = oldState?.Name;

		switch (newStateName)
		{
			case null:
				break;

			case "SplashScreenState":
				MainMenuCamera.Position = CameraInitialPosition;
				MainMenuCamera.Rotation = CameraInitialRotation;
				CirclingVultures.Restart();
				MainMenuScreenAssembly.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
				DelayBeforeSplashScreenTransitionTimer.Start(DelayBeforeSplashScreenTransition);
				break;

			case "SplashScreenTransitioningState":
				break;

			case "SplashScreenAwaitingState":
				break;

			case "TitleScreenState":
				break;

			case "TitleScreenTransitioningState":
				break;

			case "MainMenuScreenState":
				break;

			default:
				break;
		}
	}

	private void _on_delay_before_splash_screen_transition_timer_timeout()
	{
		MainMenuStateMachine.EnterState("SplashScreenTransitioningState");
	}

	private void _on_delay_before_continue_text_timer_timeout()
	{
		MainMenuStateMachine.EnterState("SplashScreenAwaitingState");
	}

}
