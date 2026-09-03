using Godot;
using System;
using Godot.Collections;

public partial class MultiplayerLobbyMenu : Control
{
	[Export] public PackedScene MultiplayerSessionButton { get; set; }
	[Export] public Node NodeToAddChildrenOf { get; set; }
	[Export] public Timer SessionListenerTimer { get; set; }
	[Export] public ScrollContainer SessionsScrollContainer { get; set; }
	[Export] public Button DirectHostJoinButton { get; set; }
	public bool CheckingForNewSessions { get; set; } = false;
	private Array<LanDiscoverySession> _lastFrameKnownSessions = new Array<LanDiscoverySession>();

    public override void _Process(double delta)
    {
        if (CheckingForNewSessions)
		{
			CheckForNewSessions();
		}
    }

	public void AddSessionButton(string ipAddress, int portAddress)
	{
		SessionButton newButton = MultiplayerSessionButton.Instantiate<SessionButton>();
		NodeToAddChildrenOf.AddChild(newButton);
		newButton.Configure(ipAddress, portAddress);
	}

	// Immidiately frees all session buttons from memory.
	public void ClearSessionButtons()
	{
		foreach(SessionButton button in GetSessionButtons())
		{
			button.Free();
		}
	}

	// Hides all session buttons to prevent selection, and the queues
	// them for deletion from memory on the next process frame.
	public void ClearSessionButtons(bool deleteDeferred = false)
	{
		if (deleteDeferred)
		{
			foreach(SessionButton button in GetSessionButtons())
			{
				button.Visible = false;
				button.QueueFree();
			}
		}
	}

	public Array<SessionButton> GetSessionButtons()
	{
		Array<SessionButton> sessionButtons = new Array<SessionButton>();
		foreach (Node child in NodeToAddChildrenOf.GetChildren())
		{
			if (child is SessionButton sessionButton)
			{
				sessionButtons.Add(sessionButton);
			}
		}
		return sessionButtons;
	}

	// Checks the network manager for any known sessions that are not stored
	// in the copy stored the prior frame. If found, they are added as SessionButtons
	// and the method returns true.
	public bool CheckForNewSessions()
	{
		Array<LanDiscoverySession> currentSessions = NetworkManager.GetKnownSessions();
		bool foundNew = false;

		foreach (LanDiscoverySession session in currentSessions)
		{
			bool alreadyKnown = false;
			foreach (LanDiscoverySession lastFrameSession in _lastFrameKnownSessions)
			{
				if (lastFrameSession.Ip == session.Ip && lastFrameSession.Port == session.Port)
				{
					alreadyKnown = true;
					break;
				}
			}

			if (alreadyKnown)
			{
				continue;
			}

			AddSessionButton(session.Ip, session.Port);
			foundNew = true;
		}

		_lastFrameKnownSessions = currentSessions.Duplicate();
		return foundNew;
	}

	private void _on_back_button_pressed()
	{
		Visible = false;
	}

	// Makes a LAN discovery broadcast. This clears the list of known sessions as well.
	// Over the next 4 seconds, all newly-recognized hosted sessions will be added as
	// a SessionButton.
	private void _on_refresh_button_pressed()
	{
		ClearSessionButtons();
		_lastFrameKnownSessions.Clear();
		CheckingForNewSessions = true;
		SessionListenerTimer.Start(4.0);
		NetworkManager.BroadcastToListeningHosts();
	}

	private void _on_direct_host_join_button_pressed()
	{
		SessionsScrollContainer.Visible = false;
		DirectHostJoinButton.Disabled = true;
	}

	private void _on_session_listener_timer_timeout()
	{
		CheckingForNewSessions = false;
	}
}
