using Godot;
using System;

public partial class NetworkManager : Node
{
    [Export] public int Port { get; set; } = 7777;
    [Export] public int MaxClients { get; set; } = 4;
	
    public bool IsHost => Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer();

    public override void _Ready()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    public Error Host()
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error err = peer.CreateServer(Port, MaxClients);
        if (err != Error.Ok)
        {
            Debug.LogError("Failed to host: " + err);
            return err;
        }

        Multiplayer.MultiplayerPeer = peer;
        Debug.Log("Hosting on port " + Port + " as peer " + Multiplayer.GetUniqueId());
        return Error.Ok;
    }

    public Error Join(String address)
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        Error err = peer.CreateClient(address, Port);
        if (err != Error.Ok)
        {
            Debug.LogError("Failed to join: " + err);
            return err;
        }

        Multiplayer.MultiplayerPeer = peer;
        Debug.Log("Connecting to " + address + ":" + Port);
        return Error.Ok;
    }

    public void DisconnectSession()
    {
        if (Multiplayer.MultiplayerPeer is null)
        {
            return;
        }

        Multiplayer.MultiplayerPeer.Close();
        Multiplayer.MultiplayerPeer = null;
        Debug.Log("Disconnected");
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
