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
    public const string MainMenu = "res://scenes/trainwrecked_main_menu.tscn";
    public const string TargetLevel = "res://scenes/testing_scene.tscn";
    /// TODO: Implement.
    public const string StaticLoadingScreen = "res://scenes/static_loading_screen.tscn";

    /// <summary>
    /// Singleton set in <see cref="_Ready"/>. Used so the host can invoke RPCs on this node.
    /// </summary>
    public static LevelLoader Instance { get; set; }

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
    /// (including locally via <c>CallLocal</c>). Offline singleplayer loading is not implemented.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when offline with no session (singleplayer load unimplemented), or when called
    /// as a connected client.
    /// </exception>
    public static void LoadMainLevel()
    {
        if (!NetworkManager.IsConnected() && !NetworkManager.IsConnecting())
        {
            throw new InvalidOperationException("Unimplemented singleplayer level load.");
        }

        if (NetworkManager.IsClient())
        {
            throw new InvalidOperationException("Attempted to load level as a non-host.");
        }

        if (NetworkManager.IsServer())
        {
            Instance.Rpc(MethodName.ChangeSceneToPackedTargetLevel);
        }
    }

    /// <summary>
    /// Loads the main menu. When hosting a session, broadcasts to all peers; when offline
    /// (including a client after host loss), changes the scene locally.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called as a connected client (host must drive the session-wide return),
    /// or when the local scene change fails.
    /// </exception>
    public static void LoadMainMenu()
    {
        if (NetworkManager.IsConnected() || NetworkManager.IsConnecting())
        {
            if (NetworkManager.IsClient())
            {
                throw new InvalidOperationException("Attempted to load main menu as a non-host while connected.");
            }

            if (NetworkManager.IsServer())
            {
                Instance.Rpc(MethodName.ChangeSceneToPackedMainMenu);
                return;
            }
        }

        Error error = Instance.GetTree().ChangeSceneToFile(MainMenu);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to load main menu: {error}.");
        }
    }

    /// <summary>
    /// Authority RPC that switches every peer's scene tree to <see cref="TargetLevel"/>.
    /// </summary>
    /// <remarks>
    /// Callable only by the multiplayer authority, with <c>CallLocal</c> so the host
    /// also changes scenes. Invoked from <see cref="LoadMainLevel"/> on the server.
    /// </remarks>
    /// <returns>The result of <see cref="SceneTree.ChangeSceneToFile"/>.</returns>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private Error ChangeSceneToPackedTargetLevel()
    {
        return GetTree().ChangeSceneToFile(TargetLevel);
    }

    /// <summary>
    /// Authority RPC that switches every peer's scene tree to <see cref="MainMenu"/>.
    /// </summary>
    /// <remarks>
    /// Callable only by the multiplayer authority, with <c>CallLocal</c> so the host
    /// also changes scenes. Invoked from <see cref="LoadMainMenu"/> on the server.
    /// </remarks>
    /// <returns>The result of <see cref="SceneTree.ChangeSceneToFile"/>.</returns>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private Error ChangeSceneToPackedMainMenu()
    {
        return GetTree().ChangeSceneToFile(MainMenu);
    }

}
