using System;
using Godot;

public abstract partial class ChatMessage : Label
{
	public string Message { get; }

	public ChatMessage(string message, float chatBoxWidth)
	{
		this.Message = message;
		Text = $"{message}";
		CustomMinimumSize = new Vector2(chatBoxWidth, CustomMaximumSize.X);
		AutowrapMode = TextServer.AutowrapMode.WordSmart;
	}

    public override string ToString()
    {
		string generatedInstanceString = Debug.GenerateInstanceToString(this);
        return $"{generatedInstanceString}|{Message}";
    }
}
