using Godot;
using System;
using Godot.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;

// Works in tandem with the engine native high level multiplayer API to provide
// networking service via host-client connection. Aims to support LAN-local connections
// with LAN discovery capability.
public partial class NetworkManager : Node
{
	private Array<LanDiscoverySession> _knownSessions = new Array<LanDiscoverySession>();

	public static NetworkManager Instance;
	public const string Address = "localhost";
	// The port that the host uses to recieve packets from the UDP packet peer.
	public const int DiscoveryPort = 7778;
	// The port that the session uses to host the game. It can be considered the "default" port.
	// At this time, no other port is used for multiplayer hosting/joining.
	public const int Port = 7777;
	public const int MaxClients = 4;
	public const string DiscoverMessage = "TW_DISCOVER";
	public const string HostReplyPrefix = "TW_HOST";
	// private bool _HostListening = true;
	// public bool HostListening
	// {
	// 	get {return _HostListening;}
	// 	set {SetHostListening(value);}
	// }
	// private bool _ClientListening = true;
	// public bool ClientListening
	// {
	// 	get {return _ClientListening;}
	// 	set {SetClientListening(value);}
	// }
	// The UDP packet peer used to send queries for a host to listen and answer.
	public PacketPeerUdp Query = null;

	// Wrappers for MultiplayerAPI connection lifecycle signals.
	// Listen via NetworkManager.Instance.PeerConnected += ... (or Connect).

	/// <summary>
	/// Emitted when a remote peer joins. <paramref name="id"/> is their multiplayer peer id.
	/// Typically used on the host when a client connects.
	/// </summary>
	[Signal] public delegate void PeerConnectedEventHandler(long id);

	/// <summary>
	/// Emitted when a remote peer leaves. <paramref name="id"/> is their multiplayer peer id.
	/// </summary>
	[Signal] public delegate void PeerDisconnectedEventHandler(long id);

	/// <summary>
	/// Emitted on the client when the connection to the host succeeds
	/// after <see cref="StartClient"/>.
	/// </summary>
	[Signal] public delegate void ConnectedToServerEventHandler();

	/// <summary>
	/// Emitted on the client when joining the host fails
	/// after <see cref="StartClient"/>.
	/// </summary>
	[Signal] public delegate void ConnectionFailedEventHandler();

	/// <summary>
	/// Emitted on the client when the host disconnects or the session is lost.
	/// </summary>
	[Signal] public delegate void ServerDisconnectedEventHandler();

	public override void _Ready()
	{
		Instance = this;
		Multiplayer.PeerConnected += OnMultiplayerPeerConnected;
		Multiplayer.PeerDisconnected += OnMultiplayerPeerDisconnected;
		Multiplayer.ConnectedToServer += OnMultiplayerConnectedToServer;
		Multiplayer.ConnectionFailed += OnMultiplayerConnectionFailed;
		Multiplayer.ServerDisconnected += OnMultiplayerServerDisconnected;

		// Check for executable arguments: open a session if found.
		if (RunArguments.HasHostFlag)
		{
			StartServer();
		}
		else if (RunArguments.HasJoinFlag)
		{
			StartClient(RunArguments.JoinFlagAddress);
		}
	}

	// All methods here are event handlers that do nothing but
	// emit their wrapper signals.
	#region wrapper signal emitters
	private void OnMultiplayerPeerConnected(long id)
	{
		EmitSignal(SignalName.PeerConnected, id);
	}

	private void OnMultiplayerPeerDisconnected(long id)
	{
		EmitSignal(SignalName.PeerDisconnected, id);
	}

	private void OnMultiplayerConnectedToServer()
	{
		EmitSignal(SignalName.ConnectedToServer);
	}

	private void OnMultiplayerConnectionFailed()
	{
		EmitSignal(SignalName.ConnectionFailed);
	}

	private void OnMultiplayerServerDisconnected()
	{
		EmitSignal(SignalName.ServerDisconnected);
	}

	#endregion

	public override void _Process(double delta)
	{
		ListenForClientsAsHost();
		ListenForHostsAsClient();
	}

	// Recieve incoming packets, and when the prefix signurature is detected,
	// respond with the host response prefix packet.
	public void ListenForClientsAsHost()
	{
		if (Query is null)
		{
			return;
		}

		// Return if not a host or offline.
		if (!IsServer() || !IsConnected())
		{
			return;
		}

		while (Query.GetAvailablePacketCount() > 0)
		{
			string message = Query.GetPacket().GetStringFromUtf8();
			if (message != DiscoverMessage)
			{
				continue;
			}

			string fromIp = Query.GetPacketIP();
			int fromPort = Query.GetPacketPort();
			Query.SetDestAddress(fromIp, fromPort);
			Query.PutPacket($"{HostReplyPrefix}|{Port}".ToUtf8Buffer());
			Debug.Log("Recieved a client prefix, replying to client.");
		}
	}

	public static void BroadcastToListeningHosts()
	{
		// If the host tries to call this method return to prevent
		// overwriting the packet destination.
		if (IsConnected() && IsServer())
		{
			return;
		}

		if (Instance.Query is null)
		{
			Instance.Query = new PacketPeerUdp();
			Instance.Query.Bind(0);
			Instance.Query.SetBroadcastEnabled(true);
		}

		// Clear the list of known sessions.
		Instance._knownSessions.Clear();

		Instance.Query.SetDestAddress("255.255.255.255", DiscoveryPort);
		Instance.Query.PutPacket(DiscoverMessage.ToUtf8Buffer());
	}

	public void ListenForHostsAsClient()
	{
		if (Query is null)
		{
			return;
		}

		// Hosts use Query as the discovery listener; do not consume their packets here.
		if (IsConnected() && IsServer())
		{
			return;
		}

		while (Query.GetAvailablePacketCount() > 0)
		{
			string message = Query.GetPacket().GetStringFromUtf8();
			string hostIp = Query.GetPacketIP();
			if (!message.StartsWith(HostReplyPrefix))
			{
				continue;
			}

			// By now it is known that the incoming message is a valid host reply.
			string stringHostPort = message.Split("|")[1];
			int.TryParse(stringHostPort, out int hostPort);

			// Instance a new session resource and add it to the array of known sessions.
			LanDiscoverySession newlyDiscoveredSession = new LanDiscoverySession(hostIp, hostPort);
			_knownSessions.Add(newlyDiscoveredSession);
		}
	}

	// Hosts a game on port 7777.
	public static Error StartServer()
	{
		Error returnError = Error.Unavailable;
		ENetMultiplayerPeer newPeer = new ENetMultiplayerPeer();
		returnError = newPeer.CreateServer(Port, MaxClients);
		// If the server hosting fails, return the error thrown.
		if (returnError != Error.Ok)
		{
			return returnError;
		}
		// Otherwise, set the peer and open the UDP packet peer connection.
		SetCurrentPeer(newPeer);
		Instance.Query = new PacketPeerUdp();
		Instance.Query.Bind(DiscoveryPort);
		return Error.Ok;
	}

	// Connects clients to the target address.
	public static Error StartClient(string address)
	{
		Error returnError = Error.Unavailable;
		ENetMultiplayerPeer newPeer = new ENetMultiplayerPeer();
		returnError = newPeer.CreateClient(address, Port);
		// If the server joining fails, return the error thrown.
		if (returnError != Error.Ok)
		{
			return returnError;
		}
		// Otherwise, set the peer.
		SetCurrentPeer(newPeer);
		return Error.Ok;
	}

	public static MultiplayerPeer GetCurrentPeer()
	{
		return Instance.Multiplayer.MultiplayerPeer;
	}

	// Sets the multiplayer API's peer property, enabling networking.
	public static void SetCurrentPeer(ENetMultiplayerPeer peer)
	{
		if (peer is null)
		{
			Debug.LogError("Set active peer to null.");
		}

		Instance.Multiplayer.MultiplayerPeer = peer;
	}

	public static void SetCurrentPeer(OfflineMultiplayerPeer peer)
	{
		if (peer is null)
		{
			Debug.LogError("Set active peer to null.");
		}

		Instance.Multiplayer.MultiplayerPeer = peer;
	}

	/// <summary>
	/// Returns the IPv4 loopback address for localhost as a string ("127.0.0.1").
	/// </summary>
	/// <returns>A string representing the IPv4 loopback address.</returns>
	public static string GetLocalHostIpv4()
	{
		return "127.0.0.1";
	}

	/// <summary>
	/// Returns whether this machine has an active ENet multiplayer session
	/// whose connection status is <see cref="MultiplayerPeer.ConnectionStatus.Connected"/>.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> when the current peer is a connected <see cref="ENetMultiplayerPeer"/>;
	/// <see langword="false"/> when offline, still connecting, or using <see cref="OfflineMultiplayerPeer"/>.
	/// </returns>
	public static bool IsConnected()
	{
		MultiplayerPeer peer = GetCurrentPeer();
		if (peer is null || peer is OfflineMultiplayerPeer)
		{
			return false;
		}

		return peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
	}

	/// <summary>
	/// Returns whether this machine is a client currently attempting to join a host.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> when the current peer is an <see cref="ENetMultiplayerPeer"/>
	/// with status <see cref="MultiplayerPeer.ConnectionStatus.Connecting"/> and this peer is not the server;
	/// otherwise <see langword="false"/>.
	/// </returns>
	public static bool IsConnecting()
	{
		MultiplayerPeer peer = GetCurrentPeer();
		if (peer is null || peer is OfflineMultiplayerPeer || IsServer())
		{
			return false;
		}

		return peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connecting;
	}

	/// <summary>
	/// Returns whether the multiplayer API treats this peer as the server.
	/// </summary>
	/// <remarks>
	/// This mirrors <see cref="MultiplayerApi.IsServer"/> and does not require a network session.
	/// <see cref="OfflineMultiplayerPeer"/> (the default) also reports as server, so pair with
	/// <see cref="IsConnected"/> when you need "hosting an online game."
	/// </remarks>
	/// <returns>
	/// <see langword="true"/> if this peer is the server (including offline default);
	/// otherwise <see langword="false"/>.
	/// </returns>
	public static bool IsServer()
	{
		return Instance.Multiplayer.IsServer();
	}

	/// <summary>
	/// Returns whether this machine is a connected multiplayer client.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> when <see cref="IsConnected"/> is true and <see cref="IsServer"/> is false;
	/// otherwise <see langword="false"/> (including while still connecting).
	/// </returns>
	public static bool IsClient()
	{
		if (IsConnected() && !IsServer())
		{
			return true;
		}

		return false;
	}

	// On a host, closes the server entirely.
	// On a client, disconnects from the server.
	public static void Disconnect()
	{
		if (IsServer() && IsConnected())
		{
			GetCurrentPeer().Close();
			SetCurrentPeer(new OfflineMultiplayerPeer());
			if (Instance.Query is not null)
			{
				Instance.Query.Close();
				Instance.Query = null;
			}
		}
		else if (IsClient() && IsConnected())
		{
			GetCurrentPeer().Close();
			SetCurrentPeer(new OfflineMultiplayerPeer());
			if (Instance.Query is not null)
			{
				Instance.Query.Close();
				Instance.Query = null;
			}
		}
	}

	public static Array<LanDiscoverySession> GetKnownSessions()
	{
		return Instance._knownSessions;
	}

	// Returns THIS client's LAN IPv4 that other devices on the same subnet can reach.
	// Prefers private ranges (10/8, 172.16/12, 192.168/16). Returns "" if none found.
	public static string GetPublicSubnetIpv4()
	{
		string fallback = "";

		foreach (string address in IP.GetLocalAddresses())
		{
			if (!IPAddress.TryParse(address, out IPAddress parsed))
			{
				continue;
			}

			if (parsed.AddressFamily != AddressFamily.InterNetwork)
			{
				continue;
			}

			if (IPAddress.IsLoopback(parsed))
			{
				continue;
			}

			byte[] bytes = parsed.GetAddressBytes();

			// Skip link-local 169.254.0.0/16
			if (bytes[0] == 169 && bytes[1] == 254)
			{
				continue;
			}

			bool isPrivate =
				bytes[0] == 10
				|| (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
				|| (bytes[0] == 192 && bytes[1] == 168);

			if (isPrivate)
			{
				return address;
			}

			if (fallback == "")
			{
				fallback = address;
			}
		}

		return fallback;
	}

	public static int GetUniqueId()
	{
		return Instance.Multiplayer.GetUniqueId();
	}

	public static Array<int> GetPeerIds()
	{
		return new Array<int>(Instance.Multiplayer.GetPeers());
	}

	public static void DisconnectAllOtherPeers()
	{
		foreach (int somePeer in GetPeerIds())
		{
			GetCurrentPeer().DisconnectPeer(somePeer);
		}
	}

	// Returns the amount of players (including the host and self) currently connected.
	// Does not include players that are currently still connecting.
	public static int GetTotalConnectedPeerCount()
	{
		if (!IsConnected())
		{
			return 0;
		}

		// GetPeers() / GetPeerIds() omit this peer.
		return GetPeerIds().Count + 1;
	}

}
