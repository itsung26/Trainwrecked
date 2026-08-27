using Godot;
using System;

public partial class NetworkManager : Node
{
	public const string DEFAULTIP = "127.0.0.1";

	public static bool IsHost
	{
		get
		{
			MultiplayerApi Api = GetMultiplayerApi();
			return Api.MultiplayerPeer != null && Api.IsServer();
		}
	}

	public static int ClientCount
	{
		get
		{
			MultiplayerApi Api = GetMultiplayerApi();
			if (Api.MultiplayerPeer is null)
			{
				return 0;
			}

			return Api.GetPeers().Length;
		}
	}

	public override void _Ready()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
	}

	public static Error Host(int Port = 7777, int MaxClients = 4)
	{
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error err = peer.CreateServer(Port, MaxClients);
		if (err != Error.Ok)
		{
			Debug.LogError("Failed to host: " + err);
			return err;
		}

		MultiplayerApi Api = GetMultiplayerApi();
		Api.MultiplayerPeer = peer;
		Debug.Log("Hosting on port " + Port + " as peer " + Api.GetUniqueId());
		return Error.Ok;
	}

	public static Error Join(String Address, int Port = 7777)
	{
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error err = peer.CreateClient(Address, Port);
		if (err != Error.Ok)
		{
			Debug.LogError("Failed to join: " + err);
			return err;
		}

		GetMultiplayerApi().MultiplayerPeer = peer;
		Debug.Log("Connecting to " + Address + ":" + Port);
		return Error.Ok;
	}

	public static void DisconnectSession()
	{
		MultiplayerApi Api = GetMultiplayerApi();
		if (Api.MultiplayerPeer is null)
		{
			return;
		}

		Api.MultiplayerPeer.Close();
		Api.MultiplayerPeer = null;
		Debug.Log("Disconnected");
	}

	private static MultiplayerApi GetMultiplayerApi()
	{
		return ((SceneTree)Engine.GetMainLoop()).GetMultiplayer();
	}

	private void OnPeerConnected(long id)
	{
		Debug.Log("Peer connected: " + id + " (I am " + Multiplayer.GetUniqueId() + ")");
	}

	private void OnPeerDisconnected(long id)
	{
		GD.Print("Peer disconnected: ", id);
	}

	private void OnConnectedToServer()
	{
		GD.Print("Connected to host. My id is ", Multiplayer.GetUniqueId());
	}

	private void OnConnectionFailed()
	{
		GD.PrintErr("Connection failed");
		Multiplayer.MultiplayerPeer = null;
	}

	private void OnServerDisconnected()
	{
		GD.Print("Host left");
		Multiplayer.MultiplayerPeer = null;
	}
}
