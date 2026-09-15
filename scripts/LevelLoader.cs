using Godot;
using Godot.Collections;
using System;
using System.Threading.Tasks;

// Handles scene load synchronization and player spawning between clients.
// There is only 1 level, which will change its properties and generate based off of parameters
// rather than swapping out scenes per biome/area etc...
public partial class LevelLoader : Node
{
    public readonly PackedScene TargetLevel;
    public readonly PackedScene StaticLoadingScreen;
    public static LevelLoader Instance { get; set; }
    
    public LevelLoader()
    {
        TargetLevel = GD.Load<PackedScene>("res://scenes/testing_scene.tscn");
        StaticLoadingScreen = GD.Load<PackedScene>("res://scenes/static_loading_screen.tscn");
    }

    public override void _Ready()
    {
        Instance = this;
    }

    // Loads the level. The client is never allowed to change the scene for everyone.
    // The host will always begin all clients' scene change behavior.
    public static Error LoadMainLevel()
    {
        if (!NetworkManager.IsConnected() && !NetworkManager.IsConnecting())
        {
            Debug.LogError("Unimplemented singleplayer level load");
            return Error.Unconfigured;
        }

        if (NetworkManager.IsClient())
        {
            Debug.LogError("Attempted to load level as a non-host.");
            return Error.Unauthorized;
        }
        else if (NetworkManager.IsServer())
        {
            Instance.Rpc(MethodName.ChangeSceneToPackedTargetLevel);
        }

        return Error.Ok;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private Error ChangeSceneToPackedTargetLevel()
    {
        return GetTree().ChangeSceneToPacked(TargetLevel);
    }

}
