using Godot;
using System;
using Godot.Collections;

/// <summary>
/// Owns child <see cref="State"/> nodes and switches among them.
/// </summary>
/// <remarks>
/// Do not add or remove state children at runtime. States are collected once in
/// <see cref="_Ready"/> via <see cref="InitializeStates"/>.
/// </remarks>
[GlobalClass]
public partial class StateMachine : Node
{
	/// <summary>When <see langword="true"/>, logs state initialization and transitions.</summary>
	[Export] public bool LoggingDebug = false;
	// Determines the first state entered. If null, initial state is intended to be entered manually.
	[Export] public State InitialState { get; set; }

	/// <summary>Child <see cref="State"/> nodes discovered at initialization.</summary>
	public Array<State> States = new Array<State>();

	/// <summary>The state currently entered, or <see langword="null"/> if none.</summary>
	public State CurrentState = null;

	/// <summary>The state exited by the most recent transition, or <see langword="null"/>.</summary>
	public State PreviousState = null;

	/// <summary>
	/// Emitted after a successful transition.
	/// <paramref name="NewState"/> is the state just entered;
	/// <paramref name="OldState"/> is the previous state (may be <see langword="null"/>).
	/// </summary>
	[Signal] public delegate void StateChangedEventHandler(State NewState, State OldState);

	/// <summary>Collects child states when the node enters the scene tree.</summary>
	public override void _Ready()
	{
		InitializeStates();
	}

	/// <summary>
	/// Populates <see cref="States"/> from direct children that are <see cref="State"/> nodes.
	/// </summary>
	private void InitializeStates()
	{
		Array<Node> Children = GetChildren();
		for (int i = 0; i < Children.Count; i++)
		{
			Node Child = Children[i];
			if (Child is State)
			{
				States.Add(Child as State);
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
	/// <param name="StateToEnter">State to enter. Ignored when <see langword="null"/>.</param>
	/// <remarks>
	/// If <paramref name="StateToEnter"/> is already current and
	/// <see cref="State.CanBeReentered"/> is <see langword="false"/>, this is a no-op.
	/// </remarks>
	public void EnterState(State StateToEnter)
	{
		if (StateToEnter == null)
		{
			return;
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

		String PreviousName = PreviousState != null ? PreviousState.Name : "None";
		if (LoggingDebug)
		{
			Debug.Log("Entered state: " + StateToEnter.Name + " from previous state: " + PreviousName);
		}
		EmitSignal(SignalName.StateChanged, StateToEnter, PreviousState);
	}

	/// <summary>
	/// Enters the child state whose <see cref="Node.Name"/> equals <paramref name="StateName"/>
	/// (case-insensitive).
	/// </summary>
	/// <param name="StateName">Name of the state to enter. Ignored when <see langword="null"/>.</param>
	/// <remarks>
	/// Resolves the state with <see cref="GetStateByName"/>, then calls
	/// <see cref="EnterState(State)"/>. Missing names result in a no-op.
	/// </remarks>
	public void EnterState(String StateName)
	{
		if (StateName == null) return;
		State StateToEnter = GetStateByName(StateName);
		EnterState(StateToEnter);
	}

	/// <summary>
	/// Enters the initialized child state at <paramref name="stateIndex"/>.
	/// </summary>
	/// <param name="stateIndex">Index into <see cref="States"/>.</param>
	/// <remarks>
	/// Resolves the state with <see cref="GetStateByIndex"/>, then calls
	/// <see cref="EnterState(State)"/>. Out-of-range indices result in a no-op.
	/// </remarks>
	public void EnterState(int stateIndex)
	{
		State stateToEnter = GetStateByIndex(stateIndex);
		EnterState(stateToEnter);
	}

	/// <summary>
	/// Returns the initialized child state whose <see cref="Node.Name"/> equals
	/// <paramref name="StateName"/> (case-insensitive).
	/// </summary>
	/// <param name="StateName">Name to match against <see cref="States"/>.</param>
	/// <returns>The matching <see cref="State"/>, or <see langword="null"/> if none is found.</returns>
	public State GetStateByName(String StateName)
	{
		for (int i = 0; i < States.Count; i++)
		{
			State State = States[i];
			if (string.Equals(State.Name, StateName, StringComparison.OrdinalIgnoreCase))
			{
				return State;
			}
		}
		return null;
	}

	/// <summary>
	/// Returns the initialized child state at <paramref name="index"/>.
	/// </summary>
	/// <param name="index">Index into <see cref="States"/>.</param>
	/// <returns>The matching <see cref="State"/>, or <see langword="null"/> if the index is out of range.</returns>
	public State GetStateByIndex(int index)
	{
		if (index < 0 || index >= States.Count)
		{
			return null;
		}
		return States[index];
	}
}
