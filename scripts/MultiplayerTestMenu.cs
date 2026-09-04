using Godot;
using System;

public partial class MultiplayerTestMenu : Control
{
	[Export] PackedScene LevelToLoad { get; set; }
	[Export] Button HostButton { get; set; }
	[Export] Button JoinButton { get; set; }
	[Export] Label HostOrClientLabel { get; set; }
	[Export] Label ClientCountLabel { get; set; }
	[Export] string ConnectingAddress { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateHostOrClientLabel();
		UpdateClientCountLabel();
		Debug.Log(NetworkManager.GetPublicSubnetIpv4());
	}

	private void UpdateHostOrClientLabel()
	{
		if (NetworkManager.IsConnected())
		{
			if (NetworkManager.IsServer())
			{
				HostOrClientLabel.Text = "SERVER";
			}
			else
			{
				HostOrClientLabel.Text = "CLIENT";
			}
		}
		else
		{
			HostOrClientLabel.Text = "DISCONNECTED";
		}
	}

	private void UpdateClientCountLabel()
	{
		
	}

	// Returns true if this host has at least one client connected.
	private bool HasPeer()
	{
		if (!NetworkManager.IsConnected() || !NetworkManager.IsServer())
		{
			return false;
		}

		return Multiplayer.GetPeers().Length > 0;
	}

	private void _on_host_button_pressed()
	{
		NetworkManager.StartServer();
	}

	private void _on_join_button_pressed()
	{
		NetworkManager.StartClient(ConnectingAddress);
	}

	private void _on_disconnect_button_pressed()
	{
		NetworkManager.DisconnectThisClient();
	}

	private void _on_broadcast_to_hosts_button_pressed()
	{
		NetworkManager.BroadcastToListeningHosts();
	}

	private void _on_read_host_replies_button_pressed()
	{
		Debug.Log(NetworkManager.GetKnownSessions());
	}
}
