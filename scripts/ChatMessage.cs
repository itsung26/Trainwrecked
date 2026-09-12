using Godot;
using System;

// Represents a message sent to the chat. Senders are identified by their
// unique multiplayer ID.
public partial class ChatMessage : Label
{
	public string Message { get; }
	public int Sender { get; }

	public ChatMessage(string message, int sender, float chatBoxWidth)
	{
		Message = message;
		Text = $"{sender}: {message}";
		Sender = sender;
		CustomMinimumSize = new Vector2(chatBoxWidth, CustomMaximumSize.X);
		AutowrapMode = TextServer.AutowrapMode.WordSmart;
	}

    public override string ToString()
    {
		string generatedInstanceString = Debug.GenerateInstanceToString(this);
        return $"{generatedInstanceString}|{Sender}: {Message}";
    }

}
