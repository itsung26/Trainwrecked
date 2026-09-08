using Godot;
using System;
using Godot.Collections;
using System.Text.RegularExpressions;

public partial class MultiplayerLobbyMenu : Control
{
	[Export] public PackedScene MultiplayerSessionButton { get; set; }
	[Export] public Node NodeToAddChildrenOf { get; set; }
	[Export] public Timer SessionListenerTimer { get; set; }
	[Export] public ScrollContainer SessionsScrollContainer { get; set; }
	[Export] public Control DirectHostJoinMenu { get; set; }
	[Export] public Button DirectHostJoinButton { get; set; }
	[Export] public Button LanSessionsButton { get; set; }
	[Export] public Button HostButton { get; set; }
	[Export] public Button JoinButton { get; set; }
	[Export] public Label InvalidIpAddressLabel { get; set; }
	[Export] public Label BlankLabel { get; set; }
	[Export] public Control LanSessionsMenu { get; set; }
	[Export] public Label HostingStatusLabel { get; set; }
	[Export] public LineEdit IpInput { get; set; }
	[Export] public LineEdit NameInput { get; set; }
	[Export] public Button DisconnectButton { get; set; }
	[Export] public Label PublicIpLabel { get; set; }
	[Export] public PanelContainer JoinersPanel { get; set; }
	[Export] public Label ClientLabel1 { get; set; }
	[Export] public Label ClientLabel2 { get; set; }
	[Export] public Label ClientLabel3 { get; set; }
	[Export] public Label ClientLabel4 { get; set; }
	[Export] public Label UniqueIdLabel { get; set; }
	public bool CheckingForNewSessions { get; set; } = false;
	private Array<LanDiscoverySession> _lastFrameKnownSessions = new Array<LanDiscoverySession>();

	public override void _Ready()
	{
		// Initialize by setting initial state.
		_on_direct_host_join_button_pressed();
		HostingStatusLabel.Text = "";
		NetworkManager.Instance.PeerConnected += _on_peer_connected;
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
		NetworkManager.Instance.ConnectionFailed += _on_connection_failed;
		NetworkManager.Instance.ServerDisconnected += _on_server_disconnected;
		NetworkManager.Instance.PeerDisconnected += _on_peer_disconnected;
		PublicIpLabel.Text = "Your hosting IP is: " + NetworkManager.GetPublicSubnetIpv4();
		// Clear the joiner labels.
		ClientLabel1.Text = "";
		ClientLabel2.Text = "";
		ClientLabel3.Text = "";
		ClientLabel4.Text = "";
	}

	public override void _Process(double delta)
	{
		if (CheckingForNewSessions)
		{
			CheckForNewSessions();
		}
		UniqueIdLabel.Text = "My unique ID: " + NetworkManager.GetUniqueId();
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
		foreach (SessionButton button in GetSessionButtons())
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
			foreach (SessionButton button in GetSessionButtons())
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

	// Returns true if newInput is in the valid form ###.###.###.###
	// Returns false otherwise.
	private bool ValidateIpTextInput(string newInput)
	{
		if (newInput is null)
		{
			return false;
		}

		newInput = newInput.Substr(0, 15);

		// Regex regex = new Regex("[0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+", RegexOptions.IgnoreCase);
		return Regex.IsMatch(newInput, @"^\d{1,3}(\.\d{1,3}){3}$");
	}

	// Sets the text of all ClientLabels to newText.
	private void SetAllClientLabelText(string newText)
	{
		ClientLabel1.Text = newText;
		ClientLabel2.Text = newText;
		ClientLabel3.Text = newText;
		ClientLabel4.Text = newText;
	}

	// Returns the first Clientlabel, etc ClientLabel1, ClientLabel2,... that has only the text
	// "". If they all have text, return null.
	private Label GetFirstFreeClientLabel()
	{
		if (ClientLabel1.Text == "")
		{
			return ClientLabel1;
		}
		if (ClientLabel2.Text == "")
		{
			return ClientLabel2;
		}
		if (ClientLabel3.Text == "")
		{
			return ClientLabel3;
		}
		if (ClientLabel4.Text == "")
		{
			return ClientLabel4;
		}

		return null;
	}

	// Returns the first ClientLabel with a name matching uniqueIdName.
	private Label GetClientLabel(string uniqueIdName)
	{
		if (ClientLabel1.Text == uniqueIdName || ClientLabel1.Text.StartsWith(uniqueIdName + " "))
		{
			return ClientLabel1;
		}
		if (ClientLabel2.Text == uniqueIdName || ClientLabel2.Text.StartsWith(uniqueIdName + " "))
		{
			return ClientLabel2;
		}
		if (ClientLabel3.Text == uniqueIdName || ClientLabel3.Text.StartsWith(uniqueIdName + " "))
		{
			return ClientLabel3;
		}
		if (ClientLabel4.Text == uniqueIdName || ClientLabel4.Text.StartsWith(uniqueIdName + " "))
		{
			return ClientLabel4;
		}

		return null;
	}

	// Returns the amount of client labels in the list that do not contain
	// only the text "".
	private int GetClientLabelsOccupied()
	{
		int amt = 0;
		if (ClientLabel1.Text != "")
		{
			amt++;
		}
		if (ClientLabel2.Text != "")
		{
			amt++;
		}
		if (ClientLabel3.Text != "")
		{
			amt++;
		}
		if (ClientLabel4.Text != "")
		{
			amt++;
		}

		return amt;
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
		DirectHostJoinButton.Disabled = true;
		LanSessionsButton.Disabled = false;
		LanSessionsMenu.Visible = false;
		DirectHostJoinMenu.Visible = true;
	}

	private void _on_lan_sessions_button_pressed()
	{
		LanSessionsButton.Disabled = true;
		DirectHostJoinMenu.Visible = false;
		LanSessionsMenu.Visible = true;
		DirectHostJoinButton.Disabled = false;

	}

	private void _on_ip_input_text_changed(string newText)
	{
		JoinButton.Disabled = !ValidateIpTextInput(newText);
		InvalidIpAddressLabel.Visible = !ValidateIpTextInput(newText);
		BlankLabel.Visible = ValidateIpTextInput(newText);

		if (newText == "")
		{
			BlankLabel.Visible = true;
			InvalidIpAddressLabel.Visible = false;
		}
	}

	private void _on_host_button_pressed()
	{
		JoinButton.Disabled = true;
		Error returnedError = NetworkManager.StartServer();

		if (returnedError == Error.Ok)
		{
			HostButton.Visible = false;
			DisconnectButton.Visible = true;
			IpInput.Editable = false;
			IpInput.Text = NetworkManager.GetPublicSubnetIpv4();
			// Set the first label to show the hosts' ID.
			if (GetFirstFreeClientLabel() is not null)
			{
				GetFirstFreeClientLabel().Text = NetworkManager.GetUniqueId().ToString() + " " + "(HOST)";
				JoinersPanel.GetChild(0).GetChild<Label>(0).Text = $"Clients Connected {GetClientLabelsOccupied()}/4";
			}
			HostingStatusLabel.Text = "Successfully hosted server on IP " + NetworkManager.GetPublicSubnetIpv4() + " with port 7777";
		}
		else if (returnedError != Error.Ok)
		{
			HostingStatusLabel.Text = "Error hosting server: " + returnedError.ToString();
		}
	}

	// Precondition: The input IP will always be a valid IP string,
	// but may not be a real IP address.
	private void _on_join_button_pressed()
	{
		JoinButton.Disabled = true;
		HostButton.Disabled = true;
		IpInput.Editable = false;
		JoinButton.Text = "Joining...";
		Error returnedError = NetworkManager.StartClient(IpInput.Text);
		if (returnedError != Error.Ok)
		{
			Debug.Log("Error joining: " + returnedError);
		}
	}

	private void _on_disconnect_button_pressed()
	{
		NetworkManager.Disconnect();
		HostingStatusLabel.Text = "";
		SetAllClientLabelText("");
		JoinersPanel.GetChild(0).GetChild<Label>(0).Text = "Clients Connected 0/4";

		if (NetworkManager.IsServer())
		{
			DisconnectButton.Visible = false;
			HostButton.Visible = true;
			IpInput.Clear();
			IpInput.Editable = true;
		}
	}

	private void _on_session_listener_timer_timeout()
	{
		CheckingForNewSessions = false;
	}

	private void _on_peer_connected(long id)
	{
		// If host, update the list of clients.
		// ClientListSynchronizer will keep the list in sync for clients.
		if (NetworkManager.IsServer())
		{
			if (GetFirstFreeClientLabel() is not null)
			{
				GetFirstFreeClientLabel().Text = id.ToString() + " " + "(CLIENT)";
			}
			JoinersPanel.GetChild(0).GetChild<Label>(0).Text = $"Clients Connected {GetClientLabelsOccupied()}/4";
		}
	}

	private void _on_connected_to_server()
	{
		HostingStatusLabel.Text = "Successfully connected to session.";
		JoinButton.Text = "Join";
		JoinButton.Disabled = true;
		HostButton.Disabled = false;
		HostButton.Visible = false;
		DisconnectButton.Visible = true;
	}

	private void _on_connection_failed()
	{
		HostingStatusLabel.Text = "Failed to connect to session. Ensure you are on the same local subnet as the host.";
		JoinButton.Text = "Join";
		JoinButton.Disabled = false;
		HostButton.Disabled = false;
		IpInput.Clear();
		IpInput.Editable = true;
		// Wipe away the dead EnetMultiplayerPeer from the failed connection attempt.
		NetworkManager.GetCurrentPeer().Close();
		NetworkManager.SetCurrentPeer(new OfflineMultiplayerPeer());
	}

	private void _on_server_disconnected()
	{
		int unique = NetworkManager.GetUniqueId();
		Debug.Log($"{unique} registered a server disconnection. It is expected that either the host left or the session was lost.");
		// Clear all labels.
		SetAllClientLabelText("");
		JoinersPanel.GetChild(0).GetChild<Label>(0).Text = "Clients Connected 0/4";
	}

	private void _on_peer_disconnected(long id)
	{
		if (NetworkManager.IsServer())
		{
			Label labelOfClientThatLeft = GetClientLabel(id.ToString());
			labelOfClientThatLeft.Text = "";
		}
	}
}
