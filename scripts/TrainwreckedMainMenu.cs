using System;
using Godot;

public partial class TrainwreckedMainMenu : Control
{
	[Export] public float ContinueLabelPulseFrequency { get; set; } = 1.0f;
	[Export] public float CameraMaxShiftOffset { get; set; } = 1.0f; // How much the camera is allowed to move around with the mouse.


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
	[Export] public Camera3D MainMenuCamera { get; set; }

	// Used to calculate offset anchors so that the camera can move
	// with the mouse in the final state. Updated upon entry to the
	// final state.

	private Transform3D _cameraFinalTransform = new Transform3D();

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
		else if (StateMachine.CurrentState.Name == "MainMenuDone")
		{
			MoveCameraWithMouse();
		}
	}

	public void UpdateButtonRestrictions()
	{
		bool isClient = NetworkManager.IsClient();

		NewGameButton.Disabled = isClient;
		LoadButton.Disabled = isClient;
	}

	private void MoveCameraWithMouse()
	{
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		Vector2 mousePos = GetViewport().GetMousePosition();
		Vector2 mouseNormalized = new Vector2(mousePos.X / viewportSize.X, 1.0f - mousePos.Y / viewportSize.Y);
		mouseNormalized = mouseNormalized.Clamp(Vector2.Zero, Vector2.One);
		float mouseDistanceFromCenter = mouseNormalized.DistanceTo(Vector2.Zero);

		Vector3 cameraFinalPosition = _cameraFinalTransform.Origin;

		// Guaranteed to be coplanar.

		Vector3 cameraUpAnchor = cameraFinalPosition + _cameraFinalTransform.Basis.Y.Normalized() * CameraMaxShiftOffset;
		Vector3 cameraDownAnchor = cameraFinalPosition - _cameraFinalTransform.Basis.Y.Normalized() * CameraMaxShiftOffset;
		Vector3 cameraRightAnchor = cameraFinalPosition + _cameraFinalTransform.Basis.X.Normalized() * CameraMaxShiftOffset;
		Vector3 cameraLeftAnchor = cameraFinalPosition - _cameraFinalTransform.Basis.X.Normalized() * CameraMaxShiftOffset;

		MainMenuCamera.GlobalPosition = BilinearInterpolate(
			cameraLeftAnchor,
			cameraRightAnchor,
			cameraUpAnchor,
			cameraDownAnchor,
			mouseNormalized
		);
		
		if (Input.IsActionJustPressed("Debug Action"))
		{
			MainMenuCamera.GlobalPosition = cameraDownAnchor;
		}
	}

	private static Vector3 BilinearInterpolate(Vector3 left, Vector3 right, Vector3 top, Vector3 bottom, Vector2 uv)
	{
		Vector3 horizontal = left.Lerp(right, uv.X);
		Vector3 vertical = bottom.Lerp(top, uv.Y);
		Vector3 center = (left + right) * 0.5f;
		return horizontal + vertical - center;
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
				// Update for camera-mouse movement

				_cameraFinalTransform = MainMenuCamera.GlobalTransform;
				break;
		}
	}


	#endregion
}
