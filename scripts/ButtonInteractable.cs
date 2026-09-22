using Godot;
using System;
using Godot.Collections;
using System.Linq;

[GlobalClass]
public partial class ButtonInteractable : Interactable
{
    [Export] public string PressAnimationName { get; set; }
    private AnimationPlayer _animationPlayer = null;
    private bool _canBePressed = true;

    [Signal] public delegate void ButtonPressedEventHandler();

    public override void _Ready()
    {
        base._Ready();
        _animationPlayer = GetChildren().OfType<AnimationPlayer>().FirstOrDefault();
        if (_animationPlayer is not null)
        {
            _animationPlayer.AnimationFinished += _on_animation_player_animation_finished;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public override void Interact()
    {
        if (!_canBePressed)
        {
            return;
        }

        _canBePressed = false;
        _animationPlayer.Play(new StringName(PressAnimationName));
    }

    /// <summary>
    /// Intended to be called as part of the animation: PressAnimationName.
    /// Add a method call track and call the method when the button as at its
    /// most pressed phase.
    /// </summary>
    private void EmitPressedSignal()
    {
        EmitSignal(SignalName.ButtonPressed);
    }

    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
    }

    private void _on_animation_player_animation_finished(StringName animName)
    {
        _canBePressed = true;
    }
    

}
