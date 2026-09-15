using Godot;
using Godot.Collections;
using System;
using System.Threading.Tasks;


/// <summary>
/// Root node for the active Trainwrecked level. Host-authoritative player spawning
/// runs here; clients receive player instances via <see cref="PlayerSpawner"/>.
/// </summary>
/// <remarks>
/// Only one level should be configured at a time. Scene changes are driven by
/// <see cref="LevelLoader"/>. On a connected host, <see cref="_Ready"/> spawns peer
/// <c>1</c> first, then each id from <see cref="NetworkManager.GetPeerIds"/>.
/// </remarks>
[GlobalClass]
public partial class TrainwreckedLevel : Node3D
{
	/// <summary>
	/// Packed scene instantiated for each player. Must match a scene listed on
	/// <see cref="PlayerSpawner"/>.
	/// </summary>
	[Export] protected PackedScene PlayerScene { get; set; }

	/// <summary>
	/// Spawner that replicates player nodes added under this level to remote peers.
	/// Must be assigned and configured in the editor.
	/// </summary>
	[Export] protected MultiplayerSpawner PlayerSpawner { get; set; }

	/// <summary>
	/// Four spawn markers. Index <c>0</c> is the host spot; indices <c>1</c>–<c>3</c>
	/// are used for clients in connection order from <see cref="NetworkManager.GetPeerIds"/>.
	/// </summary>
	[Export] protected Array<Node3D> PlayerSpawnPoints { get; set; } = new Array<Node3D>();

	/// <summary>
	/// Validates exports, then on a connected server spawns the host player and all
	/// currently connected clients under this node for <see cref="PlayerSpawner"/> replication.
	/// </summary>
	public override void _Ready()
	{
		NetworkManager.Instance.PeerConnected += _on_peer_connected;
		// Export config validation.
		if (PlayerSpawner is null)
		{
			Debug.LogError($"Player spawner must be assigned and configured.");
			return;
		}
		if (PlayerSpawnPoints.Count != 4)
		{
			Debug.LogError($"Player spawn points must be of length 4.");
			return;
		}

		if (NetworkManager.IsServer() && NetworkManager.IsConnected())
		{
			// Create and spawn the host player first.
			CreatePlayer(1, 1);
			// Then spawn other players in.
			int index = 2;
			foreach (int id in NetworkManager.GetPeerIds())
			{
				CreatePlayer(id, index);
				index++;
			}
		}
	}

	public override void _Process(double delta)
	{
		
	}

	/// <summary>
	/// Instantiates a <see cref="Player"/> for <paramref name="peerId"/>, parents it under
	/// this level, and places it at the matching spawn marker.
	/// </summary>
	/// <remarks>
	/// The node name is set to the peer id string so <see cref="Player"/> can assign
	/// multiplayer authority in <c>_EnterTree</c> (required for
	/// <see cref="MultiplayerSynchronizer"/> with <see cref="MultiplayerSpawner"/>).
	/// Intended to be called on the host only; clients should not spawn players locally.
	/// </remarks>
	/// <param name="peerId">Multiplayer peer id that owns the new player.</param>
	/// <param name="playerIndex">
	/// One-based spawn spot (<c>1</c>–<c>4</c>). Spot <c>1</c> is always the host.
	/// Maps to <see cref="PlayerSpawnPoints"/> at <c>playerIndex - 1</c>.
	/// </param>
	public void CreatePlayer(int peerId, int playerIndex)
	{
		Player newPlayer = PlayerScene.Instantiate<Player>();
		newPlayer.Name = peerId.ToString();
		AddChild(newPlayer, true);
		newPlayer.GlobalPosition = PlayerSpawnPoints[playerIndex - 1].GlobalPosition;
	}

	private void _on_peer_connected(long id)
	{
		if (NetworkManager.IsServer() && NetworkManager.IsConnected())
		{
			Debug.Log("Registered late join. Spawning player.");
			int firstFreePlayerIndex = NetworkManager.GetTotalConnectedPeerCount();
			CreatePlayer((int)id, firstFreePlayerIndex);
		}
	}

	private void _on_peer_disconnected(long id)
	{
		if (NetworkManager.IsServer() && NetworkManager.IsConnected())
		{
			if (NetworkManager.IsServer() && NetworkManager.IsConnected())
			{
				Player playerToDespawn = GetNodeOrNull<Player>(id.ToString());
				if (playerToDespawn is not null)
				{
					playerToDespawn.QueueFree();
				}
			}
		}
	}
}
