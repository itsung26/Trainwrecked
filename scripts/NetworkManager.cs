using Godot;
using System;

// Works in tandem with the engine native high level multiplayer API to provide
// networking service via host-client connection.
public partial class NetworkManager : Node
{
	public static NetworkManager Instance;
	public const string Address = "localhost";
	public const int Port = 7777;
	public const int MaxClients = 4;

	public override void _Ready()
	{
		Instance = this;
	}

	public static Error StartServer()
	{
		Error returnError = Error.Unavailable;
		ENetMultiplayerPeer newPeer = new ENetMultiplayerPeer();
		returnError = newPeer.CreateServer(Port, MaxClients);
		SetCurrentPeer(newPeer);
		return returnError;
	}

	public static Error StartClient()
	{
		Error returnError = Error.Unavailable;
		ENetMultiplayerPeer newPeer = new ENetMultiplayerPeer();
		returnError = newPeer.CreateClient(Address, Port);
		SetCurrentPeer(newPeer);
		return returnError;
	}

	public static ENetMultiplayerPeer GetCurrentPeer()
	{
		if (Instance.Multiplayer.MultiplayerPeer is null)
		{
			return null;
		}

		return Instance.Multiplayer.MultiplayerPeer as ENetMultiplayerPeer;
	}

	// Sets the multiplayer API's peer property, enabling networking.
	public static void SetCurrentPeer(ENetMultiplayerPeer peer)
	{
		if (peer is null)
		{
			Debug.LogWarn("Set active peer to null. Remember to disconnect first. If this was already done than this message can be ignored.");
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

	public static bool IsConnected()
	{
		if (GetCurrentPeer() is not null)
		{
			return true;
		}
		return false;
	}

	public static bool IsServer()
	{
		return Instance.Multiplayer.IsServer();
	}

	public static void DisconnectThisClient()
	{
		GetCurrentPeer().DisconnectPeer(1);
	}

	public static void DisconnectOtherPeer()
	{
		// Only the host has the authority to directly disconnect other peers.
		if (!IsServer())
		{
			return;
		}

		// TODO: implement a way to lookup player ids from their names.
	}

	public static void QuitMultiplayerApi()
	{
		SetCurrentPeer(null);
	}
}
