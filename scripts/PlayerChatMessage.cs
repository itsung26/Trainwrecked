using Godot;
using System;

// Represents a message sent to the chat. Senders are identified by their
// unique multiplayer ID.
public partial class PlayerChatMessage : ChatMessage
{
	public int Sender { get; }

	public PlayerChatMessage(string message, int sender, float chatBoxWidth) : base(message, chatBoxWidth)
	{
		Sender = sender;
		Text = $"{sender}: {message}";
	}

    public override string ToString()
    {
		string generatedInstanceString = Debug.GenerateInstanceToString(this);
		return $"{generatedInstanceString}|{Sender}: {Message}";
    }

}
