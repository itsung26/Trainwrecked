using Godot;
using System;

public partial class MultiplayerTestMenu : Control
{
	[Export] PackedScene LevelToLoad { get; set; }
	[Export] Button HostButton { get; set; }
	[Export] Button JoinButton { get; set; }
	[Export] Label HostOrClientLabel { get; set; }
	[Export] Label ClientCountLabel { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateHostOrClientLabel();
		UpdateClientCountLabel();
	}

	private void UpdateHostOrClientLabel()
	{
		bool IsHost = NetworkManager.IsHost;
		if (IsHost)
		{
			HostOrClientLabel.Text = "Host or client: HOST";
		}
		else if (!IsHost)
		{
			HostOrClientLabel.Text = "Host or client: CLIENT";
		}
	}

	private void UpdateClientCountLabel()
	{
		ClientCountLabel.Text = "# of clients connected: " + NetworkManager.ClientCount;
	}

	private void _on_host_button_pressed()
	{
		NetworkManager.Host();
	}

	private void _on_join_button_pressed()
	{
		NetworkManager.Join(NetworkManager.DEFAULTIP, 7777);
	}
}
