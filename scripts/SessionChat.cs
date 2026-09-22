using Godot;
using System;
using System.Diagnostics;

/// <summary>
/// Session chat panel. Messages are host-authoritative:
/// clients submit text to the host, and the host broadcasts the final
/// message to every peer (including itself via <c>CallLocal</c>).
/// Offline / not-yet-connected input is appended locally only.
/// </summary>
public partial class SessionChat : Control
{
	[Export] public bool CanEnterChat { get; set; } = true;
	/// <summary>Container that owns chat message nodes.</summary>
	[Export] public VBoxContainer MessageList { get; set; }
	/// <summary>Line edit the player types into.</summary>
	[Export] public LineEdit MessageInput { get; set; }
	[Export] public PanelContainer ChatPanel { get; set; }
	[Export] public Timer DelayBeforeFadeoutTimer { get; set; }
	private float _panelOpacity = 1.0f;
	[Export]
	public float PanelOpacity
	{
		get
		{
			return _panelOpacity;
		}
		set
		{
			SetPanelOpacity(value);
		}
	}
	[Export] public float DelayBeforeFadeoutBegin { get; set; } = 1.0f;
	[Export] public float ActiveOpacity { get; set; } = 0.615f;
	[Export] public float InactiveOpacity { get; set; } = 0.25f;
	[Export] public float FadeOutSpeed { get; set; } = 0.5f;
	public bool IsTyping
	{
		get
		{
			return GetIsTyping();
		}
	}
	private bool _fadingOut = false;
	private StyleBoxFlat _panelStyleBox;

	/// <summary>Emitted when the chat input starts or stops being edited.</summary>
	[Signal] public delegate void TypingChangedEventHandler(bool isTyping);

	public override void _Ready()
	{
		CachePanelStyleBox();
		SetPanelOpacity(InactiveOpacity);
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("Chat Toggle") && !IsTyping && CanEnterChat)
		{
			MessageInput.Editable = true;
			MessageInput.Edit();
			MessageInput.CallDeferred(LineEdit.MethodName.Clear);
		}
	}

	public override void _Process(double delta)
	{
		if (_fadingOut)
		{
			if (PanelOpacity == InactiveOpacity)
			{
				_fadingOut = false;
			}
			else
			{
				PanelOpacity = Mathf.MoveToward(PanelOpacity, InactiveOpacity, FadeOutSpeed * ((float)delta));
			}
		}
	}

	/// <summary>
	/// Returns true if MessageInput is currently being edited, and false otherwise.
	/// </summary>
	/// <returns></returns>
	public bool GetIsTyping()
	{
		return MessageInput != null && MessageInput.IsEditing();
	}

	public void SetPanelOpacity(float opacity)
	{
		_panelOpacity = Mathf.Clamp(opacity, 0.0f, 1.0f);
		if (_panelStyleBox == null)
		{
			return;
		}

		Color col = _panelStyleBox.BgColor;
		_panelStyleBox.BgColor = new Color(col, _panelOpacity);
	}

	private void CachePanelStyleBox()
	{
		StyleBox source = ChatPanel.GetThemeStylebox("panel");
		_panelStyleBox = source?.Duplicate() as StyleBoxFlat;
		if (_panelStyleBox == null)
		{
			return;
		}

		ChatPanel.AddThemeStyleboxOverride("panel", _panelStyleBox);
	}

	/// <summary>
	/// Client → host submit path. Invoke only via
	/// <c>RpcId(1, MethodName.SubmitChatMessage, text, senderPeerId)</c>.
	/// Do not call directly. <see cref="MultiplayerApi.RpcMode.AnyPeer"/> allows
	/// any peer to send; <c>CallLocal = false</c> so the caller does not run this
	/// body. The host validates and fans out with <see cref="RecieveChatMessage"/>.
	/// </summary>
	/// <param name="text">Message body (RPC-serializable; not a node).</param>
	/// <param name="senderPeerId">Multiplayer peer id of the author.</param>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void SubmitChatMessage(string text, int senderPeerId)
	{
		if (!NetworkManager.IsServer())
		{
			return;
		}

		Rpc(MethodName.RecieveChatMessage, text, senderPeerId);
	}

	/// <summary>
	/// Host → all peers display path. Invoke only via <c>Rpc</c> from the host
	/// (authority). <c>CallLocal = true</c> so the host also adds the message.
	/// Builds a <see cref="ChatMessage"/> and passes it to <see cref="AddChatMessage"/>.
	/// </summary>
	/// <param name="text">Message body.</param>
	/// <param name="senderPeerId">Multiplayer peer id of the author.</param>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void RecieveChatMessage(string text, int senderPeerId)
	{
		ChatMessage newChat = new ChatMessage(text, senderPeerId, MessageList.Size.X);
		AddChatMessage(newChat);
	}

	/// <summary>
	/// Adds a message node to <see cref="MessageList"/> with no networking.
	/// Refuses nodes that are already in the scene tree.
	/// </summary>
	public void AddChatMessage(ChatMessage whichMessage)
	{
		if (whichMessage.IsInsideTree())
		{
			Debug.LogError(Error.AlreadyExists);
			return;
		}
		MessageList.AddChild(whichMessage);
	}

	/// <summary>
	/// Queues all chat messages for deletion from memory.
	/// Runs locally. Not an rpc.
	/// </summary>
	public void Clear()
	{
		foreach (ChatMessage chatToDelete in MessageList.GetChildren())
		{
			chatToDelete.QueueFree();
		}
	}

	/// <summary>
	/// Clears the input, then either appends locally (disconnected / connecting),
	/// broadcasts as host, or RPCs <see cref="SubmitChatMessage"/> to peer 1 as client.
	/// </summary>
	private void _on_message_input_text_submitted(string newText)
	{
		MessageInput.Clear();
		MessageInput.ReleaseFocus();
		MessageInput.Editable = false;
		// If not in a connection state, just add the message locally.
		// Although, anyone calling this is just talking to themselves.
		if (!NetworkManager.IsConnected() || NetworkManager.IsConnecting())
		{
			AddChatMessage(new ChatMessage(newText, NetworkManager.GetUniqueId(), MessageList.Size.X));
			return;
		}

		if (NetworkManager.IsServer())
		{
			Rpc(MethodName.RecieveChatMessage, newText, NetworkManager.GetUniqueId());
		}
		else if (NetworkManager.IsClient())
		{
			RpcId(1, MethodName.SubmitChatMessage, newText, NetworkManager.GetUniqueId());
		}
	}

	private void _on_connected_to_server()
	{
		Clear();
	}

	private void _on_message_input_editing_toggled(bool toggledOn)
	{
		if (toggledOn)
		{
			DelayBeforeFadeoutTimer.Stop();
			_fadingOut = false;
			PanelOpacity = ActiveOpacity;
		}
		else
		{
			DelayBeforeFadeoutTimer.Start(DelayBeforeFadeoutBegin);
		}

		EmitSignal(SignalName.TypingChanged, toggledOn);
	}

	private void _on_delay_before_fadeout_timer_timeout()
	{
		_fadingOut = true;
	}

	private void _on_visibility_changed()
	{
		CanEnterChat = Visible;
		
	}
}
