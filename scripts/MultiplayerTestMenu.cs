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
		NetworkManager.Instance.PeerConnected += OnNetworkPeerConnected;
		NetworkManager.Instance.PeerDisconnected += OnNetworkPeerDisconnected;
		NetworkManager.Instance.ConnectedToServer += OnNetworkConnectedToServer;
		NetworkManager.Instance.ConnectionFailed += OnNetworkConnectionFailed;
		NetworkManager.Instance.ServerDisconnected += OnNetworkServerDisconnected;
		Debug.Log("MultiplayerTestMenu: NetworkManager signals connected.");
	}

	private void OnNetworkPeerConnected(long id)
	{
		Debug.Log($"PeerConnected: {id}");
	}

	private void OnNetworkPeerDisconnected(long id)
	{
		Debug.Log($"PeerDisconnected: {id}");
	}

	private void OnNetworkConnectedToServer()
	{
		Debug.Log("ConnectedToServer");
	}

	private void OnNetworkConnectionFailed()
	{
		Debug.Log("ConnectionFailed");
	}

	private void OnNetworkServerDisconnected()
	{
		Debug.Log("ServerDisconnected");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateHostOrClientLabel();
		UpdateClientCountLabel();
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
		Error err = NetworkManager.StartServer();
		Debug.Log($"Host pressed. StartServer => {err}. (PeerConnected only fires when a client joins.)");
	}

	private void _on_join_button_pressed()
	{
		Error err = NetworkManager.StartClient(ConnectingAddress);
		Debug.Log($"Join pressed. StartClient({ConnectingAddress}) => {err}. Wait for ConnectedToServer or ConnectionFailed.");
	}

	private void _on_disconnect_button_pressed()
	{
		Debug.Log("Disconnect pressed. Close() does not emit PeerDisconnected on this peer.");
		NetworkManager.Disconnect();
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
