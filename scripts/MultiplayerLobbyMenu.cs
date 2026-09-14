using Godot;
using System;
using Godot.Collections;
using System.Text.RegularExpressions;

/// <summary>
/// Lobby UI for hosting, joining, and LAN discovery.
/// </summary>
/// <remarks>
/// Session chrome (host, join, disconnect, IP field, status text, and the host's roster)
/// is applied by <see cref="ApplySessionUi"/> from <see cref="NetworkManager"/> state.
/// Button handlers and launch flags (<c>--host</c>, <c>--join</c>) both end in that refresh,
/// so the menu does not depend on which path opened the session.
/// The host writes roster labels; clients receive them from the scene's
/// <c>ClientListSynchronizer</c>.
/// </remarks>
public partial class MultiplayerLobbyMenu : Control
{
	/// <summary>Packed scene instantiated for each newly discovered LAN session.</summary>
	[Export] public PackedScene MultiplayerSessionButton { get; set; }
	/// <summary>Parent that receives discovered <see cref="SessionButton"/> children.</summary>
	[Export] public Node NodeToAddChildrenOf { get; set; }
	/// <summary>
	/// Timer that ends a LAN discovery listen window.
	/// On timeout, <see cref="CheckingForNewSessions"/> becomes <see langword="false"/>.
	/// </summary>
	[Export] public Timer SessionListenerTimer { get; set; }
	/// <summary>
	/// Scroll container around the LAN session list.
	/// Assigned in the scene; this script does not read it.
	/// </summary>
	[Export] public ScrollContainer SessionsScrollContainer { get; set; }
	/// <summary>Panel for entering an IP and hosting or joining directly.</summary>
	[Export] public Control DirectHostJoinMenu { get; set; }
	/// <summary>Tab button that shows <see cref="DirectHostJoinMenu"/>.</summary>
	[Export] public Button DirectHostJoinButton { get; set; }
	/// <summary>Tab button that shows <see cref="LanSessionsMenu"/> and starts discovery.</summary>
	[Export] public Button LanSessionsButton { get; set; }
	/// <summary>Starts a listen server via <see cref="NetworkManager.StartServer"/>.</summary>
	[Export] public Button HostButton { get; set; }
	/// <summary>Joins <see cref="IpInput"/> via <see cref="NetworkManager.StartClient"/>.</summary>
	[Export] public Button JoinButton { get; set; }
	/// <summary>Shown when <see cref="IpInput"/> is non-empty and not a dotted IPv4.</summary>
	[Export] public Label InvalidIpAddressLabel { get; set; }
	/// <summary>Spacer shown when the invalid-IP label is hidden.</summary>
	[Export] public Label BlankLabel { get; set; }
	/// <summary>Panel that lists sessions found by LAN discovery.</summary>
	[Export] public Control LanSessionsMenu { get; set; }
	/// <summary>Status line for hosting, joining, and connection errors.</summary>
	[Export] public Label HostingStatusLabel { get; set; }
	/// <summary>IP address field used to join, and to display the host address while hosting.</summary>
	[Export] public LineEdit IpInput { get; set; }
	/// <summary>
	/// Player-name field.
	/// Assigned in the scene; this script does not read it.
	/// </summary>
	[Export] public LineEdit NameInput { get; set; }
	/// <summary>Leaves the current session via <see cref="NetworkManager.Disconnect"/>.</summary>
	[Export] public Button DisconnectButton { get; set; }
	/// <summary>Displays this machine's subnet address from <see cref="NetworkManager.GetPublicSubnetIpv4"/>.</summary>
	[Export] public Label PublicIpLabel { get; set; }
	/// <summary>Panel that contains the roster title and client labels.</summary>
	[Export] public PanelContainer JoinersPanel { get; set; }
	/// <summary>First roster slot. Empty text means the slot is free.</summary>
	[Export] public Label ClientLabel1 { get; set; }
	/// <summary>Second roster slot. Empty text means the slot is free.</summary>
	[Export] public Label ClientLabel2 { get; set; }
	/// <summary>Third roster slot. Empty text means the slot is free.</summary>
	[Export] public Label ClientLabel3 { get; set; }
	/// <summary>Fourth roster slot. Empty text means the slot is free.</summary>
	[Export] public Label ClientLabel4 { get; set; }
	/// <summary>Displays <see cref="NetworkManager.GetUniqueId"/> each process frame.</summary>
	[Export] public Label UniqueIdLabel { get; set; }
	/// <summary>Hides this menu and emits <see cref="ExitedMenu"/>.</summary>
	[Export] public Button BackButton { get; set; }
	/// <summary>
	/// Whether <see cref="_Process"/> should poll <see cref="NetworkManager.GetKnownSessions"/>
	/// and add buttons for sessions not seen on the previous poll.
	/// </summary>
	/// <remarks>
	/// Set <see langword="true"/> by <see cref="RefreshLanDiscoveryBox"/>.
	/// The session-listener timeout sets it back to <see langword="false"/>.
	/// </remarks>
	public bool CheckingForNewSessions { get; set; } = false;
	private Array<LanDiscoverySession> _lastFrameKnownSessions = new Array<LanDiscoverySession>();

	/// <summary>
	/// Emitted when the back button hides this menu.
	/// </summary>
	[Signal] public delegate void ExitedMenuEventHandler();

	/// <summary>
	/// Shows the direct host/join tab, subscribes to session signals, and applies the current session.
	/// </summary>
	/// <remarks>
	/// <see cref="NetworkManager"/> is an autoload, so <c>--host</c> and <c>--join</c> have already
	/// run before this scene's <c>_Ready</c>. <see cref="ApplySessionUi"/> reads that state here
	/// instead of waiting for a button press.
	/// </remarks>
	public override void _Ready()
	{
		_on_direct_host_join_button_pressed();
		NetworkManager.Instance.PeerConnected += _on_peer_connected;
		NetworkManager.Instance.ConnectedToServer += _on_connected_to_server;
		NetworkManager.Instance.ConnectionFailed += _on_connection_failed;
		NetworkManager.Instance.ServerDisconnected += _on_server_disconnected;
		NetworkManager.Instance.PeerDisconnected += _on_peer_disconnected;
		PublicIpLabel.Text = "Your hosting IP is: " + NetworkManager.GetPublicSubnetIpv4();
		ApplySessionUi();
	}

	/// <summary>
	/// Polls LAN discovery while <see cref="CheckingForNewSessions"/> is set,
	/// and refreshes <see cref="UniqueIdLabel"/>.
	/// </summary>
	public override void _Process(double delta)
	{
		if (CheckingForNewSessions)
		{
			CheckForNewSessions();
		}
		UniqueIdLabel.Text = "My unique ID: " + NetworkManager.GetUniqueId();
	}

	/// <summary>
	/// Instantiates <see cref="MultiplayerSessionButton"/> under <see cref="NodeToAddChildrenOf"/>
	/// and configures it with the given endpoint.
	/// </summary>
	/// <param name="ipAddress">Host address shown on the button.</param>
	/// <param name="portAddress">Host port shown on the button.</param>
	public void AddSessionButton(string ipAddress, int portAddress)
	{
		SessionButton newButton = MultiplayerSessionButton.Instantiate<SessionButton>();
		NodeToAddChildrenOf.AddChild(newButton);
		newButton.Configure(ipAddress, portAddress);
	}

	/// <summary>
	/// Immediately frees every <see cref="SessionButton"/> under <see cref="NodeToAddChildrenOf"/>.
	/// </summary>
	public void ClearSessionButtons()
	{
		foreach (SessionButton button in GetSessionButtons())
		{
			button.Free();
		}
	}

	/// <summary>
	/// Hides discovered session buttons and queues them for deletion when requested.
	/// </summary>
	/// <param name="deleteDeferred">
	/// When <see langword="true"/>, each button is hidden and <c>QueueFree</c>d.
	/// When <see langword="false"/>, this overload does nothing. Use <see cref="ClearSessionButtons()"/>
	/// to free buttons immediately.
	/// </param>
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

	/// <summary>
	/// Returns the <see cref="SessionButton"/> children of <see cref="NodeToAddChildrenOf"/>.
	/// Other child types are ignored.
	/// </summary>
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

	/// <summary>
	/// Adds a <see cref="SessionButton"/> for each <see cref="LanDiscoverySession"/>
	/// that was not present on the previous poll.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if at least one button was added; otherwise <see langword="false"/>.
	/// </returns>
	/// <remarks>
	/// Always replaces the previous-poll copy with <see cref="NetworkManager.GetKnownSessions"/>,
	/// including when nothing new was found. Does not remove buttons for sessions that disappeared.
	/// </remarks>
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

	/// <summary>
	/// Returns whether <paramref name="newInput"/> looks like a dotted IPv4 address.
	/// </summary>
	/// <param name="newInput">Text from <see cref="IpInput"/>. Only the first 15 characters are tested.</param>
	/// <returns>
	/// <see langword="true"/> when the text matches <c>1–3 digits</c>, three dots, and <c>1–3 digits</c> in each remaining octet;
	/// <see langword="false"/> for <see langword="null"/> or any other shape. Octet range is not checked.
	/// </returns>
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

	private void UpdateJoinersTitle()
	{
		JoinersPanel.GetChild(0).GetChild<Label>(0).Text = $"Clients Connected {GetClientLabelsOccupied()}/4";
	}

	private void ClearRoster()
	{
		SetAllClientLabelText("");
		UpdateJoinersTitle();
	}

	/// <summary>
	/// Rewrites the four roster labels from the current host session.
	/// </summary>
	/// <remarks>
	/// Slot text is <c>"{id} (HOST)"</c> for this peer and <c>"{id} (CLIENT)"</c> for each
	/// <see cref="NetworkManager.GetPeerIds"/> entry. Extra peers beyond free slots are dropped.
	/// Call only while this machine is the connected host; clients must not write these labels.
	/// </remarks>
	private void RebuildHostRoster()
	{
		SetAllClientLabelText("");
		Label hostSlot = GetFirstFreeClientLabel();
		if (hostSlot is not null)
		{
			hostSlot.Text = NetworkManager.GetUniqueId().ToString() + " (HOST)";
		}

		foreach (int peerId in NetworkManager.GetPeerIds())
		{
			Label clientSlot = GetFirstFreeClientLabel();
			if (clientSlot is null)
			{
				break;
			}

			clientSlot.Text = peerId.ToString() + " (CLIENT)";
		}

		UpdateJoinersTitle();
	}

	/// <summary>
	/// Sets host, join, disconnect, IP, status, and roster controls from the current session.
	/// </summary>
	/// <param name="statusOverride">
	/// When not <see langword="null"/>, written to <see cref="HostingStatusLabel"/> instead of the
	/// text implied by connection state. Use this for errors that state alone cannot describe.
	/// </param>
	/// <remarks>
	/// A connected host rebuilds the roster. A connected client leaves roster labels alone so
	/// <c>ClientListSynchronizer</c> can fill them. Connecting and offline both clear the roster.
	/// An empty <see cref="IpInput"/> while joining is filled from <see cref="RunArguments.JoinFlagAddress"/>
	/// when <see cref="RunArguments.HasJoinFlag"/> is set.
	/// </remarks>
	private void ApplySessionUi(string statusOverride = null)
	{
		bool isHost = NetworkManager.IsConnected() && NetworkManager.IsServer();
		bool isClient = NetworkManager.IsClient();
		bool isConnecting = NetworkManager.IsConnecting();
		bool inSession = isHost || isClient || isConnecting;

		if (isHost)
		{
			IpInput.Text = NetworkManager.GetPublicSubnetIpv4();
		}
		else if ((isClient || isConnecting) && IpInput.Text == "" && RunArguments.HasJoinFlag)
		{
			IpInput.Text = RunArguments.JoinFlagAddress;
		}

		HostButton.Visible = !isHost && !isClient;
		HostButton.Disabled = isConnecting;
		DisconnectButton.Visible = isHost || isClient;
		JoinButton.Text = isConnecting ? "Joining..." : "Join";
		JoinButton.Disabled = inSession || !ValidateIpTextInput(IpInput.Text);
		IpInput.Editable = !inSession;
		BackButton.Disabled = isConnecting;

		if (isHost)
		{
			RebuildHostRoster();
		}
		else if (!isClient)
		{
			ClearRoster();
		}

		if (statusOverride is not null)
		{
			HostingStatusLabel.Text = statusOverride;
		}
		else if (isHost)
		{
			HostingStatusLabel.Text = "Successfully hosted server on IP " + NetworkManager.GetPublicSubnetIpv4() + " with port " + NetworkManager.Port;
		}
		else if (isClient)
		{
			HostingStatusLabel.Text = "Successfully connected to session.";
		}
		else if (isConnecting)
		{
			HostingStatusLabel.Text = "Joining...";
		}
		else
		{
			HostingStatusLabel.Text = "";
		}
	}

	/// <summary>
	/// Clears discovered session buttons, broadcasts a LAN query, and listens for replies for 4 seconds.
	/// </summary>
	public void RefreshLanDiscoveryBox()
	{
		ClearSessionButtons();
		_lastFrameKnownSessions.Clear();
		CheckingForNewSessions = true;
		SessionListenerTimer.Start(4.0);
		NetworkManager.BroadcastToListeningHosts();

	}

	private void _on_back_button_pressed()
	{
		Visible = false;
		EmitSignal(SignalName.ExitedMenu);
	}

	private void _on_refresh_button_pressed()
	{
		RefreshLanDiscoveryBox();
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
		RefreshLanDiscoveryBox();
	}

	private void _on_ip_input_text_changed(string newText)
	{
		bool ipIsValid = ValidateIpTextInput(newText);
		bool inSession = NetworkManager.IsConnected() || NetworkManager.IsConnecting();
		JoinButton.Disabled = inSession || !ipIsValid;
		InvalidIpAddressLabel.Visible = !ipIsValid && newText != "";
		BlankLabel.Visible = ipIsValid || newText == "";
	}

	private void _on_host_button_pressed()
	{
		Error returnedError = NetworkManager.StartServer();
		if (returnedError == Error.Ok)
		{
			ApplySessionUi();
			return;
		}

		ApplySessionUi("Error hosting server: " + returnedError.ToString());
	}

	/// <summary>
	/// Joins the address in <see cref="IpInput"/>.
	/// </summary>
	/// <remarks>
	/// The field is expected to already match <see cref="ValidateIpTextInput"/>; it may still not be a reachable host.
	/// A failed <see cref="NetworkManager.StartClient"/> is logged and shown as status text.
	/// </remarks>
	private void _on_join_button_pressed()
	{
		Error returnedError = NetworkManager.StartClient(IpInput.Text);
		if (returnedError != Error.Ok)
		{
			Debug.Log("Error joining: " + returnedError);
			ApplySessionUi("Error joining: " + returnedError.ToString());
			return;
		}

		ApplySessionUi();
	}

	private void _on_disconnect_button_pressed()
	{
		NetworkManager.Disconnect();
		IpInput.Clear();
		ApplySessionUi();
	}

	private void _on_session_listener_timer_timeout()
	{
		CheckingForNewSessions = false;
	}

	/// <summary>
	/// Rebuilds the host roster after a remote peer joins.
	/// </summary>
	/// <remarks>
	/// No-ops unless this machine is a connected host. Clients do not write labels;
	/// <c>ClientListSynchronizer</c> copies the host's text.
	/// </remarks>
	private void _on_peer_connected(long _)
	{
		if (NetworkManager.IsConnected() && NetworkManager.IsServer())
		{
			RebuildHostRoster();
		}
	}

	private void _on_connected_to_server()
	{
		ApplySessionUi();
	}

	/// <summary>
	/// Drops the failed peer, clears the IP field, and shows a connection-failed status.
	/// </summary>
	private void _on_connection_failed()
	{
		NetworkManager.GetCurrentPeer().Close();
		NetworkManager.SetCurrentPeer(new OfflineMultiplayerPeer());
		IpInput.Clear();
		ApplySessionUi("Failed to connect to session. Ensure you are on the same local subnet as the host.");
	}

	/// <summary>
	/// Drops the dead peer, clears the IP field, and shows a session-lost status.
	/// </summary>
	private void _on_server_disconnected()
	{
		NetworkManager.GetCurrentPeer().Close();
		NetworkManager.SetCurrentPeer(new OfflineMultiplayerPeer());
		IpInput.Clear();
		ApplySessionUi("Disconnected from session. The host left or the connection was lost.");
	}

	private void _on_peer_disconnected(long _)
	{
		if (NetworkManager.IsConnected() && NetworkManager.IsServer())
		{
			RebuildHostRoster();
		}
	}
}
