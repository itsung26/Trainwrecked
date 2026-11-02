using Godot;
using System;
using System.Diagnostics;

/// <summary>
/// In-session chat panel with host-authoritative multiplayer delivery and a local fade UI.
/// </summary>
/// <remarks>
/// Connected clients submit text to peer <c>1</c> via <see cref="SubmitChatMessage"/>;
/// the host broadcasts the final line with <see cref="ReceiveChatMessage"/>
/// (<c>CallLocal = true</c> so the host also displays it). When disconnected or still
/// connecting, submitted text is appended locally only through <see cref="AddChatMessage"/>.
/// <para>
/// Lives on the level (see <see cref="TrainwreckedLevel.SessionChat"/>). Local
/// <see cref="Player"/> instances subscribe to <see cref="TypingChanged"/> to disable
/// movement and look while the input is being edited.
/// </para>
/// <para>
/// Pressing the <c>Chat Toggle</c> action focuses <see cref="MessageInput"/> when
/// <see cref="CanEnterChat"/> is <see langword="true"/> and the player is not already typing.
/// Panel background opacity moves between <see cref="ActiveOpacity"/> while editing and
/// <see cref="InactiveOpacity"/> after <see cref="DelayBeforeFadeoutBegin"/> seconds idle.
/// Connecting to a server clears existing messages via <see cref="Clear"/>.
/// </para>
/// </remarks>
public partial class SessionChat : Control
{
	/// <summary>
	/// <see langword="true"/> when the <c>Chat Toggle</c> action may open the input.
	/// </summary>
	/// <remarks>
	/// Mirrored from <see cref="CanvasItem.Visible"/> in <c>_on_visibility_changed</c>.
	/// </remarks>
	[Export] public bool CanEnterChat { get; set; } = true;

	/// <summary>
	/// Parent for instantiated <see cref="ChatMessage"/> nodes.
	/// </summary>
	[Export] public VBoxContainer MessageList { get; set; }

	/// <summary>
	/// Line edit that receives typed chat text and submit events.
	/// </summary>
	[Export] public LineEdit MessageInput { get; set; }

	/// <summary>
	/// Panel whose duplicated flat style box background alpha is driven by <see cref="PanelOpacity"/>.
	/// </summary>
	[Export] public PanelContainer ChatPanel { get; set; }

	/// <summary>
	/// One-shot timer started when editing ends; on timeout, fade toward <see cref="InactiveOpacity"/> begins.
	/// </summary>
	[Export] public Timer DelayBeforeFadeoutTimer { get; set; }

	private float _panelOpacity = 1.0f;

	/// <summary>
	/// Current chat panel background opacity in the range <c>0</c>–<c>1</c>.
	/// </summary>
	/// <remarks>
	/// Setting this property calls <see cref="SetPanelOpacity"/>. Defaults to <c>1</c> before
	/// <see cref="_Ready"/> applies <see cref="InactiveOpacity"/>.
	/// </remarks>
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

	/// <summary>
	/// Seconds to wait after leaving the input before fading the panel toward <see cref="InactiveOpacity"/>.
	/// </summary>
	[Export] public float DelayBeforeFadeoutBegin { get; set; } = 1.0f;

	/// <summary>
	/// Panel opacity while <see cref="MessageInput"/> is being edited.
	/// </summary>
	[Export] public float ActiveOpacity { get; set; } = 0.615f;

	/// <summary>
	/// Panel opacity when idle after the fade-out delay completes.
	/// </summary>
	[Export] public float InactiveOpacity { get; set; } = 0.25f;

	/// <summary>
	/// Opacity units per second used when fading from active toward <see cref="InactiveOpacity"/>.
	/// </summary>
	[Export] public float FadeOutSpeed { get; set; } = 0.5f;

	/// <summary>
	/// <see langword="true"/> when <see cref="MessageInput"/> is currently being edited.
	/// </summary>
	public bool IsTyping
	{
		get
		{
			return GetIsTyping();
		}
	}

	private bool _fadingOut = false;
	private StyleBoxFlat _panelStyleBox;

	/// <summary>
	/// Emitted when <see cref="MessageInput"/> starts or stops being edited.
	/// </summary>
	/// <param name="isTyping">
	/// <see langword="true"/> when editing began; <see langword="false"/> when editing ended.
	/// </param>
	[Signal] public delegate void TypingChangedEventHandler(bool isTyping);

	/// <summary>
	/// Caches the panel style box, applies <see cref="InactiveOpacity"/>, and hooks
	/// <see cref="NetworkManager.ConnectedToServer"/> so history clears on join.
	/// </summary>
	public override void _Ready()
	{
		CachePanelStyleBox();
		SetPanelOpacity(InactiveOpacity);
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
	}

	/// <summary>
	/// Opens <see cref="MessageInput"/> when <c>Chat Toggle</c> is pressed, chat entry is allowed,
	/// and the player is not already typing.
	/// </summary>
	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("Chat Toggle") && !IsTyping && CanEnterChat)
		{
			MessageInput.Editable = true;
			MessageInput.Edit();
			MessageInput.CallDeferred(LineEdit.MethodName.Clear);
		}
	}

	/// <summary>
	/// Advances panel opacity toward <see cref="InactiveOpacity"/> while a fade-out is active.
	/// </summary>
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
	/// Returns whether <see cref="MessageInput"/> is currently being edited.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> when <see cref="MessageInput"/> is non-null and
	/// <see cref="LineEdit.IsEditing"/> reports editing; otherwise <see langword="false"/>.
	/// </returns>
	public bool GetIsTyping()
	{
		return MessageInput != null && MessageInput.IsEditing();
	}

	/// <summary>
	/// Clamps <paramref name="opacity"/> to <c>0</c>–<c>1</c> and writes it to the panel style box background alpha.
	/// </summary>
	/// <param name="opacity">Desired opacity before clamping.</param>
	/// <remarks>
	/// No-ops when the cached style box has not been created yet.
	/// </remarks>
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

	/// <summary>
	/// Duplicates <see cref="ChatPanel"/>'s theme <c>panel</c> style box so opacity can be changed without mutating the shared theme resource.
	/// </summary>
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
	/// body. The host validates and fans out with <see cref="ReceiveChatMessage"/>.
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

		Rpc(MethodName.ReceiveChatMessage, text, senderPeerId);
	}

	/// <summary>
	/// Host → all peers display path. Invoke only via <c>Rpc</c> from the host
	/// (authority). <c>CallLocal = true</c> so the host also adds the message.
	/// Builds a <see cref="ChatMessage"/> and passes it to <see cref="AddChatMessage"/>.
	/// </summary>
	/// <param name="text">Message body.</param>
	/// <param name="senderPeerId">Multiplayer peer id of the author.</param>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void ReceiveChatMessage(string text, int senderPeerId)
	{
		ChatMessage newChat = new ChatMessage(text, senderPeerId, MessageList.Size.X);
		AddChatMessage(newChat);
	}

	/// <summary>
	/// Parents <paramref name="whichMessage"/> under <see cref="MessageList"/> with no networking.
	/// </summary>
	/// <param name="whichMessage">Message node to append. Must not already be in the scene tree.</param>
	/// <remarks>
	/// Logs <see cref="Error.AlreadyExists"/> and returns when the node is already inside the tree.
	/// </remarks>
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
	/// Queues every <see cref="ChatMessage"/> under <see cref="MessageList"/> for deletion.
	/// </summary>
	/// <remarks>
	/// Local only; not an RPC. Called when this peer connects to a server so lobby/offline lines do not carry into the session.
	/// </remarks>
	public void Clear()
	{
		foreach (ChatMessage chatToDelete in MessageList.GetChildren())
		{
			chatToDelete.QueueFree();
		}
	}

	/// <summary>
	/// Handles line-edit submit: clears and unfocuses the input, then delivers the text.
	/// </summary>
	/// <param name="newText">Submitted message body.</param>
	/// <remarks>
	/// Disconnected or connecting peers append locally. The host broadcasts with
	/// <see cref="ReceiveChatMessage"/>. Clients RPC <see cref="SubmitChatMessage"/> to peer <c>1</c>.
	/// </remarks>
	private void _on_message_input_text_submitted(string newText)
	{
		MessageInput.Clear();
		MessageInput.ReleaseFocus();
		MessageInput.Editable = false;

		// Return on empty chat message.
		if (newText.Equals(""))
		{
			return;
		}

		if (NetworkManager.IsServer())
		{
			Rpc(MethodName.ReceiveChatMessage, newText, NetworkManager.GetUniqueId());
		}
		else if (NetworkManager.IsClient())
		{
			RpcId(1, MethodName.SubmitChatMessage, newText, NetworkManager.GetUniqueId());
		}
	}

	/// <summary>
	/// Clears chat history when this peer finishes connecting to a server.
	/// </summary>
	private void _on_connected_to_server()
	{
		Clear();
	}

	/// <summary>
	/// Syncs panel opacity and fade timers with input edit state, then emits <see cref="TypingChanged"/>.
	/// </summary>
	/// <param name="toggledOn">
	/// <see langword="true"/> when editing began; <see langword="false"/> when editing ended.
	/// </param>
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

	/// <summary>
	/// Starts the idle fade toward <see cref="InactiveOpacity"/> after <see cref="DelayBeforeFadeoutBegin"/>.
	/// </summary>
	private void _on_delay_before_fadeout_timer_timeout()
	{
		_fadingOut = true;
	}

	/// <summary>
	/// Keeps <see cref="CanEnterChat"/> aligned with this control's visibility.
	/// </summary>
	private void _on_visibility_changed()
	{
		CanEnterChat = Visible;
		
	}
}
