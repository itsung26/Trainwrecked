using Godot;
using System;

public partial class PauseMenu : Control
{
	[Export] public Player Player { get; set; }
	private CanvasLayer _canvasLayerParent { get; set; }

	public override void _Ready()
	{
		_canvasLayerParent = GetParent<CanvasLayer>();
		if (_canvasLayerParent is null)
		{
			throw new InvalidOperationException("Expected CanvasLayer parent.");
		}

		_canvasLayerParent.VisibilityChanged += _on_canvas_layer_visibility_changed;
		_canvasLayerParent.Visible = false;
	}

	public override void _ExitTree()
	{

		if (_canvasLayerParent is not null)
		{
			_canvasLayerParent.VisibilityChanged -= _on_canvas_layer_visibility_changed;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		bool isTyping = Player.SessionChat.GetIsTyping();

		if (@event is InputEventKey)
		{
			if (Input.IsActionJustPressed(new StringName("Pause")) && !isTyping)
			{
				_canvasLayerParent.Visible = !_canvasLayerParent.Visible;
			}
		}
	}

	private void _on_canvas_layer_visibility_changed()
	{

		if (_canvasLayerParent is null)
		{
			return;
		}

		Input.MouseMode = _canvasLayerParent.Visible
			? Input.MouseModeEnum.Visible
			: Input.MouseModeEnum.Captured;

		Player.InputDisabled = _canvasLayerParent.Visible;
		Player.LookDisabled = _canvasLayerParent.Visible;
	}
}
