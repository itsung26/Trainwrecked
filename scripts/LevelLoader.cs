using Godot;
using Godot.Collections;
using System;
using System.Threading.Tasks;

/// <summary>
/// Coordinates host-authoritative level scene changes across multiplayer peers.
/// </summary>
/// <remarks>
/// There is a single level scene whose content is expected to vary by parameters
/// rather than swapping distinct biome/area scenes. Only the host may start a load;
/// clients receive the change via <see cref="ChangeSceneToPackedTargetLevel"/>.
/// Player spawning after the level loads is handled by <see cref="TrainwreckedLevel"/>,
/// not this node.
/// </remarks>
public partial class LevelLoader : Node
{
    public readonly PackedScene MainMenu;
    /// <summary>
    /// Packed scene changed to when <see cref="LoadMainLevel"/> succeeds on the host.
    /// Loaded from <c>res://scenes/testing_scene.tscn</c>.
    /// </summary>
    public readonly PackedScene TargetLevel;

    /// <summary>
    /// Packed scene for the static loading screen UI
    /// (<c>res://scenes/static_loading_screen.tscn</c>).
    /// </summary>
    /// <remarks>
    /// Loaded at construction but not yet applied during <see cref="LoadMainLevel"/>.
    /// </remarks>
    public readonly PackedScene StaticLoadingScreen;

    /// <summary>
    /// Singleton set in <see cref="_Ready"/>. Used so the host can invoke RPCs on this node.
    /// </summary>
    public static LevelLoader Instance { get; set; }
    
    /// <summary>
    /// Loads <see cref="TargetLevel"/> and <see cref="StaticLoadingScreen"/> from disk.
    /// </summary>
    public LevelLoader()
    {
        TargetLevel = GD.Load<PackedScene>("res://scenes/testing_scene.tscn");
        StaticLoadingScreen = GD.Load<PackedScene>("res://scenes/static_loading_screen.tscn");
        MainMenu = GD.Load<PackedScene>("res://scenes/trainwrecked_main_menu.tscn");
    }

    /// <summary>
    /// Registers this node as <see cref="Instance"/>.
    /// </summary>
    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// Begins loading the main level for all peers. Must be called on the host of an
    /// active or connecting multiplayer session; clients must not invoke this.
    /// </summary>
    /// <remarks>
    /// When this peer is the server, broadcasts <see cref="ChangeSceneToPackedTargetLevel"/>
    /// (including locally via <c>CallLocal</c>). Offline singleplayer loading is not implemented
    /// and returns <see cref="Error.Unconfigured"/>.
    /// </remarks>
    /// <returns>
    /// <see cref="Error.Ok"/> when the host issues the scene-change RPC;
    /// <see cref="Error.Unconfigured"/> when offline with no session;
    /// <see cref="Error.Unauthorized"/> when called as a connected client.
    /// </returns>
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

    /// <summary>
    /// Authority RPC that switches every peer's scene tree to <see cref="TargetLevel"/>.
    /// </summary>
    /// <remarks>
    /// Callable only by the multiplayer authority, with <c>CallLocal</c> so the host
    /// also changes scenes. Invoked from <see cref="LoadMainLevel"/> on the server.
    /// </remarks>
    /// <returns>The result of <see cref="SceneTree.ChangeSceneToPacked"/>.</returns>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private Error ChangeSceneToPackedTargetLevel()
    {
        return GetTree().ChangeSceneToPacked(TargetLevel);
    }

}
