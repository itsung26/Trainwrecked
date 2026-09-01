using Godot;
using System;

public partial class MultiplayerLobbyMenu : Control
{
	[Export] public PackedScene MultiplayerSessionButton { get; set; }
	[Export] public Node NodeToAddChildrenOf { get; set; }

	public void AddSessionButton(string ipAddress, int portAddress)
	{
		SessionButton newButton = MultiplayerSessionButton.Instantiate<SessionButton>();
		NodeToAddChildrenOf.AddChild(newButton);
		newButton.Configure(ipAddress, portAddress);
	}

	private void _on_back_button_pressed()
	{
		Hide();
	}

	private void _on_refresh_button_pressed()
	{
		if (NetworkManager.IsServer())
		{
			return;
		}
	}
}
