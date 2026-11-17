using Godot;
using System;

/// <summary>
/// Autoload that owns application shutdown so the window close button and in-game quit share one path.
/// </summary>
/// <remarks>
/// Project setting <c>config/auto_accept_quit</c> is <see langword="false"/>, so Godot does not quit
/// on its own when the window close is requested. This node handles
/// <see cref="Window.CloseRequested"/>, tears down multiplayer via <see cref="NetworkManager.Disconnect"/>,
/// then calls <see cref="SceneTree.Quit()"/>. Use <see cref="RequestClose"/> for the same sequence from UI.
/// </remarks>
public partial class ExitHandler : Node
{
    /// <summary>
    /// Singleton set in <see cref="_Ready"/> for the autoload instance.
    /// </summary>
    public static ExitHandler Instance { get; private set; }

    /// <summary>
    /// Registers this node as <see cref="Instance"/> and subscribes to the root window's close request.
    /// </summary>
    public override void _Ready()
    {
        Instance = this;
        GetWindow().CloseRequested += _on_close_requested;
    }

    /// <summary>
    /// Unsubscribes from the root window's close request when this node leaves the tree.
    /// </summary>
    public override void _ExitTree()
    {
        GetWindow().CloseRequested -= _on_close_requested;
    }

    /// <summary>
    /// Runs the shared shutdown sequence: disconnect multiplayer, then quit the application.
    /// </summary>
    /// <remarks>
    /// Safe entry point for menus and other UI; equivalent to the user requesting a window close.
    /// </remarks>
    public static void RequestClose()
    {
        Instance._on_close_requested();
    }

    /// <summary>
    /// Disconnects networking and quits the scene tree.
    /// </summary>
    private void _on_close_requested()
    {
        NetworkManager.Disconnect();

        GetTree().Quit();
    }

}
