using Godot;
using System;

// The chat panel. All messages entered are forwarded to
// the host, who pushes the message to all other clients.
public partial class SessionChat : Control
{
	[Export] VBoxContainer MessageList { get; set; }
	[Export] LineEdit MessageInput { get; set; }

	// Call this as the primary way to send a chat message.
	// Call as rpc. Never call directly.
	// Clients are meant to call this method as rpc for the host to run.
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	private void SubmitChatMessage(string text, int senderPeerId)
	{
		if (!NetworkManager.IsServer())
		{
			return;
		}

		Rpc(MethodName.RecieveChatMessage, text, senderPeerId);
	}

	// When called via rpc, this method will be called on all peers and add a chat message.
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void RecieveChatMessage(string text, int senderPeerId)
	{
		ChatMessage newChat = new ChatMessage(text, senderPeerId, MessageList.Size.X);
		AddChatMessage(newChat);
	}

	// Locally adds a chat message to the list.
	public void AddChatMessage(ChatMessage whichMessage)
	{
		if (whichMessage.IsInsideTree())
		{
			Debug.LogError(Error.AlreadyExists);
			return;
		}
		MessageList.AddChild(whichMessage);
	}

	private void _on_message_input_text_submitted(string newText)
	{
		MessageInput.Clear();
		// If not in a connection state, just add the message locally.
		// Although, anyone calling this is just talking to themselves.
		if (!NetworkManager.IsConnected() || NetworkManager.IsConnecting())
		{
			AddChatMessage(new ChatMessage(newText, NetworkManager.GetUniqueId(), MessageList.Size.X));
		}

		if (NetworkManager.IsServer())
		{
			Rpc(MethodName.RecieveChatMessage, newText, NetworkManager.GetUniqueId());
		}
		else
		{
			RpcId(1, MethodName.SubmitChatMessage, newText, NetworkManager.GetUniqueId());
		}
	}
}
