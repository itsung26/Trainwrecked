using Godot;
using System;
using Godot.Collections;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Node that owns child <see cref="State"/> instances and switches which one is active.
/// </summary>
/// <remarks>
/// Direct children that are <see cref="State"/> nodes are collected once during
/// <see cref="_Ready"/> into <see cref="States"/>. When <see cref="InitialState"/> is set,
/// that state is entered before <c>_Ready</c> returns. Any child added after the machine
/// is ready is rejected; states must already be in the scene at startup.
/// <para>
/// A transition calls <see cref="State.OnExit"/> on the outgoing state when one is active,
/// then <see cref="State.OnEnter"/> on the incoming state, and emits <see cref="StateChanged"/>.
/// Asking to enter <see cref="CurrentState"/> again is ignored unless
/// <see cref="State.CanBeReentered"/> is <see langword="true"/>, in which case exit and enter both run.
/// </para>
/// </remarks>
[GlobalClass]
public partial class StateMachine : Node
{
	/// <summary>
	/// <see langword="true"/> to log initialization and each transition through <see cref="Debug.Log{T}(T)"/>.
	/// </summary>
	[Export] public bool LoggingDebug { get; set; } = false;

	/// <summary>
	/// State entered at the end of <see cref="_Ready"/> when it is set.
	/// </summary>
	/// <remarks>
	/// Must be one of the states collected into <see cref="States"/>. A value that is not a
	/// collected child causes <see cref="EnterState(State)"/> to throw during startup.
	/// When unset, <see cref="CurrentState"/> stays unset until a later enter call.
	/// </remarks>
	[Export] public State InitialState { get; set; }

	/// <summary>
	/// Direct child <see cref="State"/> nodes collected during <see cref="_Ready"/>, in child order.
	/// </summary>
	public Array<State> States { get; } = new Array<State>();

	/// <summary>
	/// State most recently entered, or <see langword="null"/> before the first successful transition.
	/// </summary>
	public State CurrentState { get; private set; }

	/// <summary>
	/// State that was current before the latest transition.
	/// </summary>
	/// <remarks>
	/// <see langword="null"/> when the first state was entered with no prior current state.
	/// On a permitted re-entry this is the same instance as <see cref="CurrentState"/>.
	/// </remarks>
	public State PreviousState { get; private set; }

	/// <summary>
	/// Emitted after a state is entered, including the first entry and a permitted re-entry.
	/// </summary>
	/// <param name="NewState">State that just received <see cref="State.OnEnter"/>.</param>
	/// <param name="OldState">
	/// Previous <see cref="CurrentState"/>, or <see langword="null"/> on the first transition.
	/// On a permitted re-entry this is the same instance as <paramref name="NewState"/>.
	/// </param>
	/// <remarks>
	/// Stays silent when <see cref="EnterState(State)"/> returns early because the requested
	/// state is already current and <see cref="State.CanBeReentered"/> is <see langword="false"/>.
	/// </remarks>
	[Signal] public delegate void StateChangedEventHandler(State NewState, State OldState);

	/// <summary>
	/// Collects child states, rejects later children, and enters <see cref="InitialState"/> when it is set.
	/// </summary>
	public override void _Ready()
	{
		ChildEnteredTree += _on_state_machine_child_entered_tree;

		InitializeStates();

		if (InitialState is not null)
		{
			EnterState(InitialState);
		}
	}

	/// <summary>
	/// Adds each direct child that is a <see cref="State"/> to <see cref="States"/>.
	/// </summary>
	/// <remarks>
	/// Children that are not states are skipped. When <see cref="LoggingDebug"/> is
	/// <see langword="true"/>, logs the count and each state name. Children added after this
	/// run are rejected by <see cref="_on_state_machine_child_entered_tree"/>.
	/// </remarks>
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
	/// Exits the active state when one exists, then enters <paramref name="StateToEnter"/>.
	/// </summary>
	/// <param name="StateToEnter">Child state to make current. Must already be in <see cref="States"/>.</param>
	/// <exception cref="ArgumentNullException"><paramref name="StateToEnter"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">
	/// <paramref name="StateToEnter"/> was not collected as a child state in <see cref="_Ready"/>.
	/// </exception>
	/// <remarks>
	/// When <paramref name="StateToEnter"/> is already <see cref="CurrentState"/> and
	/// <see cref="State.CanBeReentered"/> is <see langword="false"/>, returns immediately and
	/// leaves <see cref="StateChanged"/> unemitted. Otherwise <see cref="PreviousState"/> becomes
	/// the outgoing state, <see cref="CurrentState"/> becomes <paramref name="StateToEnter"/>,
	/// and <see cref="StateChanged"/> is emitted. When <see cref="LoggingDebug"/> is
	/// <see langword="true"/>, the transition is logged.
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
	/// Enters the collected state whose <see cref="Node.Name"/> equals <paramref name="StateName"/>.
	/// </summary>
	/// <param name="StateName">Node name resolved with <see cref="GetState(string)"/>. Comparison is ordinal.</param>
	/// <exception cref="ArgumentNullException"><paramref name="StateName"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">No collected state has that name.</exception>
	/// <remarks>Delegates to <see cref="EnterState(State)"/>, including its re-entry rules.</remarks>
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
	/// Enters the state at <paramref name="stateIndex"/> in <see cref="States"/>.
	/// </summary>
	/// <param name="stateIndex">Zero-based index in child collection order.</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="stateIndex"/> is negative or greater than or equal to the number of collected states.
	/// </exception>
	/// <remarks>Delegates to <see cref="EnterState(State)"/>, including its re-entry rules.</remarks>
	public void EnterState(int stateIndex)
	{
		State stateToEnter = GetState(stateIndex);
		EnterState(stateToEnter);
	}

	/// <summary>
	/// Returns the collected state whose <see cref="Node.Name"/> equals <paramref name="StateName"/>.
	/// </summary>
	/// <param name="StateName">Node name to match. Comparison is ordinal and case-sensitive.</param>
	/// <returns>The matching state, or <see langword="null"/> when none matches.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="StateName"/> is <see langword="null"/>.</exception>
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

	/// <summary>
	/// Returns the collected state at <paramref name="index"/>.
	/// </summary>
	/// <param name="index">Zero-based index in child collection order.</param>
	/// <returns>The state at <paramref name="index"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="index"/> is negative or greater than or equal to the number of collected states.
	/// </exception>
	public State GetState(int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, States.Count);

		return States[index];
	}

	private void _on_state_machine_child_entered_tree(Node node)
	{
		if (node is State)
		{
			Debug.LogWarn("Adding states at runtime is not supported.");
		}
		else
		{
			Debug.LogError("Non-state child detected.");
		}
	}
}
