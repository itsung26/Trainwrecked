using Godot;
using System;

public partial class ServerChatMessage : ChatMessage
{
    public ServerChatMessage(string message, float chatBoxWidth) : base(message, chatBoxWidth)
	{
		Text = $"{message}";
		CustomMinimumSize = new Vector2(chatBoxWidth, CustomMaximumSize.X);
		AutowrapMode = TextServer.AutowrapMode.WordSmart;
	}

    public override string ToString()
    {
        return base.ToString();
    }
}
