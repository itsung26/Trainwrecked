using Godot;
using System;

public partial class PauseMenu : Control
{
	[Export] public Player Player { get; set; }
	[Export] public CanvasLayer CanvasLayerParent { get; set; }

	public override void _Ready()
	{
		CanvasLayerParent ??= GetParentOrNull<CanvasLayer>();
		if (CanvasLayerParent is null)
		{
			Debug.LogError("Expected a CanvasLayer parent.");
			return;
		}

		CanvasLayerParent.VisibilityChanged += _on_canvas_layer_visibility_changed;
		CanvasLayerParent.Visible = false;
	}

	public override void _ExitTree()
	{

		if (CanvasLayerParent is not null)
		{
			CanvasLayerParent.VisibilityChanged -= _on_canvas_layer_visibility_changed;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		if (@event is not InputEventKey)
		{
			return;
		}

		if (!Input.IsActionJustPressed(new StringName("Pause")))
		{
			return;
		}

		if (CanvasLayerParent is null)
		{
			Debug.LogError("Expected a CanvasLayer parent.");
			return;
		}

		CanvasLayerParent.Visible = !CanvasLayerParent.Visible;
	}

	private void _on_canvas_layer_visibility_changed()
	{

		if (CanvasLayerParent is null)
		{
			return;
		}

		Input.MouseMode = CanvasLayerParent.Visible
			? Input.MouseModeEnum.Visible
			: Input.MouseModeEnum.Captured;

		Player.InputDisabled = CanvasLayerParent.Visible;
		Player.LookDisabled = CanvasLayerParent.Visible;
	}
}
