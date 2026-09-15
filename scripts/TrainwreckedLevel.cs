using Godot;
using Godot.Collections;
using System;


// Represents the level in the Trainwrecked game there should only be one of these
// configured at a time. (see LevelLoader)
[GlobalClass]
public partial class TrainwreckedLevel : Node3D
{
	[Export] protected PackedScene PlayerScene { get; set; }
	[Export] protected MultiplayerSpawner PlayerSpawner { get; set; }
	// The primary spawn point (first element) will always be the host's spawn point.
	[Export] protected Array<Node3D> PlayerSpawnPoints { get; set; } = new Array<Node3D>();

	public override void _Ready()
	{
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

	// Creates and spawns a player belonging to peerId.
	// playerIndex represents which "spot" the player has. The host always has spot 1.
	// Name the node as the peer id; Player sets authority in _EnterTree (required for MultiplayerSynchronizer).
	public void CreatePlayer(int peerId, int playerIndex)
	{
		Player newPlayer = PlayerScene.Instantiate<Player>();
		newPlayer.Name = peerId.ToString();
		AddChild(newPlayer, true);
		newPlayer.GlobalPosition = PlayerSpawnPoints[playerIndex - 1].GlobalPosition;
	}
}
