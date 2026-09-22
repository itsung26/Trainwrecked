using Godot;
using System;
using Godot.Collections;

/// <summary>
/// Rigid interactable that one peer at a time can hold.
/// </summary>
/// <remarks>
/// Pickup and drop are host-authoritative. The host accepts or rejects claims,
/// then broadcasts hold state so every peer sets <see cref="Interactable.Selectable"/>
/// to <see langword="false"/> while held and <see langword="true"/> when free.
/// <see cref="ApplyHoldState"/> assigns <see cref="Player.CurrentHeldBody"/> on the
/// holder. Follow-the-holder transform attachment is left as stubs for a later pass.
/// </remarks>
[GlobalClass]
public partial class PickupableBody : Interactable
{
	public enum HoldFaceAxis
	{
		PositiveX,
		NegativeX,
		PositiveZ,
		NegativeZ
	}

	/// <summary>Preferred local axis facing the holder when attachment is implemented.</summary>
	[Export] public HoldFaceAxis PreferredHoldFace { get; set; } = HoldFaceAxis.NegativeZ;

	/// <summary>Maximum distance from the hold target before the body should catch up.</summary>
	[Export] public float MaxHoldDistance { get; set; } = 2.0f;

	/// <summary>Speed used when returning toward the hold target.</summary>
	[Export] public float ReturnSpeed { get; set; } = 8.0f;

	[Export] public float WalkSpeedMultiplier { get; set; } = 1.0f;

	/// <summary>Peer id of the current holder, or <c>0</c> when free.</summary>
	public int HolderPeerId { get; private set; } = 0;

	/// <summary>Whether any peer currently holds this body.</summary>
	public bool IsHeld => HolderPeerId != 0;

	public override void _Ready()
	{
		base._Ready();
		Selectable = true;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

		if (!NetworkManager.IsServer())
		{
			return;
		}

		Player holderPlayer = NetworkManager.GetPlayerByPeerId(HolderPeerId);
		if (holderPlayer is not null)
		{
			Vector3 toTarget = holderPlayer.PickupableBodyTarget.GlobalPosition - GlobalPosition;
			float distance = toTarget.Length();

			// Drop if too far.
			if (distance > MaxHoldDistance)
			{
				ServerRelease(HolderPeerId);
				return;
			}

			if (distance < 0.05f)
			{
				LinearVelocity = Vector3.Zero;
			}
			else
			{
				LinearVelocity = toTarget.Normalized() * Mathf.Min(ReturnSpeed * distance, distance / (float)delta);
			}
		}
    }

	/// <summary>
	/// Requests pickup when free, or drop when the sender already holds this body.
	/// </summary>
	/// <remarks>
	/// Invoked via <c>Rpc</c> from <see cref="Player"/> (same pattern as
	/// <see cref="ButtonInteractable"/>). Only the host applies the claim; it then
	/// replicates hold state to every peer. While held, <see cref="Interactable.Selectable"/>
	/// is <see langword="false"/> on all peers (including the holder).
	/// </remarks>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public override void Interact()
	{
		if (!NetworkManager.IsServer())
		{
			return;
		}

		int senderPeerId = Multiplayer.GetRemoteSenderId();
		if (senderPeerId == 0)
		{
			senderPeerId = NetworkManager.GetUniqueId();
		}

		if (IsHeld)
		{
			if (HolderPeerId == senderPeerId)
			{
				ServerRelease(senderPeerId);
			}
			return;
		}

		ServerClaim(senderPeerId);
	}

	/// <summary>
	/// Host-only: grants the body to <paramref name="peerId"/> when free.
	/// </summary>
	private void ServerClaim(int peerId)
	{
		if (!NetworkManager.IsServer() || IsHeld || peerId == 0)
		{
			return;
		}

		Rpc(MethodName.ApplyHoldState, true, peerId);
	}

	/// <summary>
	/// Host-only: releases the body when <paramref name="peerId"/> is the holder.
	/// </summary>
	private void ServerRelease(int peerId)
	{
		if (!NetworkManager.IsServer() || !IsHeld || HolderPeerId != peerId)
		{
			return;
		}

		Rpc(MethodName.ApplyHoldState, false, 0);
	}

	/// <summary>
	/// Applies hold state on every peer. Host broadcasts via <c>Rpc</c> with
	/// <c>CallLocal</c> so the host runs this too.
	/// </summary>
	/// <param name="held"><see langword="true"/> when claimed; <see langword="false"/> when free.</param>
	/// <param name="holderPeerId">Holder peer id, or <c>0</c> when free.</param>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void ApplyHoldState(bool held, int holderPeerId)
	{
		if (held)
		{
			HolderPeerId = holderPeerId;
			Selectable = false;
			BeginHold();

			Player holder = NetworkManager.GetPlayerByPeerId(holderPeerId);
			if (holder is not null)
			{
				holder.CurrentHeldBody = this;
			}
		}
		else
		{
			int previousHolderPeerId = HolderPeerId;
			Player holder = NetworkManager.GetPlayerByPeerId(previousHolderPeerId);
			if (holder is not null && holder.CurrentHeldBody == this)
			{
				holder.CurrentHeldBody = null;
			}

			HolderPeerId = 0;
			EndHold();
			Selectable = true;
		}
	}

	/// <summary>
	/// Local enter-hold hook. Disables physics simulation for now; attachment comes later.
	/// </summary>
	protected virtual void BeginHold()
	{
		Player player = NetworkManager.GetPlayerByPeerId(HolderPeerId);
		if (player is not null)
		{
			player.GlobalSpeedModifier = player.GlobalSpeedModifier * WalkSpeedMultiplier;
		}
		GravityScale = 0.0f;
	}

	/// <summary>
	/// Local exit-hold hook. Re-enables physics simulation for now.
	/// </summary>
	protected virtual void EndHold()
	{
		Player player = NetworkManager.GetPlayerByPeerId(HolderPeerId);
		if (player is not null)
		{
			player.GlobalSpeedModifier = 1.0f;
		}
		GravityScale = 1.0f;
	}

	public override string ToString()
	{
		return Debug.GenerateInstanceToString(this);
	}
}
