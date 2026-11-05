using Godot;
using System;
using Godot.Collections;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Owns child <see cref="State"/> nodes and switches among them.
/// </summary>
/// <remarks>
/// Do not add or remove state children at runtime. States are collected once in
/// <see cref="_Ready"/> via <see cref="InitializeStates"/>. If <see cref="InitialState"/>
/// is assigned, it is entered automatically after that collection.
/// </remarks>
[GlobalClass]
public partial class StateMachine : Node
{
	/// <summary>When <see langword="true"/>, logs state initialization and transitions.</summary>
	[Export] public bool LoggingDebug { get; set; } = false;

	/// <summary>
	/// The state to enter first after initialization.
	/// When <see langword="null"/>, the initial state must be entered manually.
	/// </summary>
	[Export] public State InitialState { get; set; }

	/// <summary>Child <see cref="State"/> nodes discovered at initialization.</summary>
	public Array<State> States { get; } = new Array<State>();

	/// <summary>The state currently entered, or <see langword="null"/> if none.</summary>
	public State CurrentState { get; private set; }

	/// <summary>The state exited by the most recent transition, or <see langword="null"/>.</summary>
	public State PreviousState { get; private set; }

	/// <summary>
	/// Emitted after a successful transition.
	/// <paramref name="NewState"/> is the state just entered;
	/// <paramref name="OldState"/> is the previous state (may be <see langword="null"/>).
	/// </summary>
	[Signal] public delegate void StateChangedEventHandler(State NewState, State OldState);

	/// <summary>
	/// Collects child states, then enters <see cref="InitialState"/> when it is set.
	/// </summary>
	public override void _Ready()
	{
		InitializeStates();
		if (InitialState is not null)
		{
			EnterState(InitialState);
		}
	}

	/// <summary>
	/// Populates <see cref="States"/> from direct children that are <see cref="State"/> nodes.
	/// </summary>
	private void InitializeStates()
	{
		foreach (Node Child in GetChildren())
		{
			if (Child is State state)
			{
				States.Add(state);
			}
		}
		if (LoggingDebug)
		{
			Debug.Log("Initialized " + States.Count + " states.");
			for (int i = 0; i < States.Count; i++)
			{
				State State = States[i];
				Debug.Log("State: " + State.Name);
			}
		}
	}

	/// <summary>
	/// Exits <see cref="CurrentState"/> (if any), enters <paramref name="StateToEnter"/>,
	/// then emits <see cref="SignalName.StateChanged"/>.
	/// </summary>
	/// <param name="StateToEnter">A child state of this machine.</param>
	/// <remarks>
	/// Throws <see cref="ArgumentNullException"/> when <paramref name="StateToEnter"/> is <see langword="null"/>.
	/// Throws <see cref="ArgumentException"/> when <paramref name="StateToEnter"/> is not one of this machine's child states.
	/// If <paramref name="StateToEnter"/> is already current and
	/// <see cref="State.CanBeReentered"/> is <see langword="false"/>, this is a no-op.
	/// </remarks>
	public void EnterState(State StateToEnter)
	{
		ArgumentNullException.ThrowIfNull(StateToEnter);
		if (!States.Contains(StateToEnter))
		{
			throw new ArgumentException($"State '{StateToEnter.Name}' is not a child of this state machine.", nameof(StateToEnter));
		}

		if (StateToEnter == CurrentState)
		{
			if (!StateToEnter.CanBeReentered)
			{
				return;
			}
		}

		// exit the previous state, if it is not null
		if (CurrentState != null)
		{
			CurrentState.OnExit();
		}

		// update the previous and current states
		PreviousState = CurrentState;
		CurrentState = StateToEnter;

		// enter the new state
		StateToEnter.OnEnter();

		string PreviousName = PreviousState != null ? PreviousState.Name : "None";
		if (LoggingDebug)
		{
			Debug.Log("Entered state: " + StateToEnter.Name + " from previous state: " + PreviousName);
		}
		EmitSignal(SignalName.StateChanged, StateToEnter, PreviousState);
	}

	/// <summary>
	/// Enters the child state whose <see cref="Node.Name"/> equals <paramref name="StateName"/>
	/// (case-sensitive).
	/// </summary>
	/// <param name="StateName">Name of the state to enter.</param>
	/// <remarks>
	/// Resolves the state with <see cref="GetState(string)"/>, then calls
	/// <see cref="EnterState(State)"/>.
	/// Throws <see cref="ArgumentNullException"/> when <paramref name="StateName"/> is <see langword="null"/>.
	/// Throws <see cref="ArgumentException"/> when no child state has that name.
	/// </remarks>
	public void EnterState(string StateName)
	{
		State StateToEnter = GetState(StateName);
		if (StateToEnter == null)
		{
			throw new ArgumentException($"No state named '{StateName}' exists on this state machine.", nameof(StateName));
		}
		EnterState(StateToEnter);
	}

	/// <summary>
	/// Enters the initialized child state at <paramref name="stateIndex"/>.
	/// </summary>
	/// <param name="stateIndex">Index into <see cref="States"/>.</param>
	/// <remarks>
	/// Resolves the state with <see cref="GetState(int)"/>, then calls
	/// <see cref="EnterState(State)"/>.
	/// Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="stateIndex"/> is outside <see cref="States"/>.
	/// </remarks>
	public void EnterState(int stateIndex)
	{
		State stateToEnter = GetState(stateIndex);
		EnterState(stateToEnter);
	}

	public State GetState(string StateName)
	{
        ArgumentNullException.ThrowIfNull(StateName);

        for (int i = 0; i < States.Count; i++)
		{
			State State = States[i];
			if (string.Equals(State.Name, StateName, StringComparison.Ordinal))
			{
				return State;
			}
		}
		return null;
	}

	public State GetState(int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, States.Count);
		return States[index];
	}
}
