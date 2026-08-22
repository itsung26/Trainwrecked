using Godot;
using System;
using Godot.Collections;

// Do not add or remove states at runtime. States are initialized as children.
[GlobalClass]
public partial class StateMachine : Node
{
	[Export] public bool LoggingDebug = false;

	public Array<State> States = new Array<State>();
	public State CurrentState = null;
	public State PreviousState = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InitializeStates();
	}

	public override void _Process(double delta)
	{
		// Debug.Log(States);
	}

	private void InitializeStates()
	{
		Array<Node> Children = GetChildren();
		for (int i = 0; i < Children.Count; i++)
		{
			Node Child = Children[i];
			if (Child is State) {
				States.Add(Child as State);
			}
		}
		Debug.Log("Initialized " + States.Count + " states.");
		for (int i = 0; i < States.Count; i++)
		{
			State State = States[i];
			Debug.Log("State: " + State.Name);
		}
	}

	// Enters the given state by reference.
	// If it is already current and canBeReentered is false, this is a no-op.
	public void EnterState(State StateToEnter)
	{
		if (StateToEnter == null) {
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
		if (CurrentState != null) {
			CurrentState.OnExit();
		}

		// update the previous and current states
		PreviousState = CurrentState;
		CurrentState = StateToEnter;

		// enter the new state
		StateToEnter.OnEnter();

		String PreviousName = PreviousState != null ? PreviousState.Name : "None";
		Debug.Log("Entered state: " + StateToEnter.Name + " from previous state: " + PreviousName);
	}

	// Enters the state with the given name.
	public void EnterState(String StateName)
	{
		if (StateName == null) return;
		State StateToEnter = GetStateByName(StateName);
		EnterState(StateToEnter);
	}

	// Returns the child state with the given name.
	// Returns null if no state with the given name is found.
	public State GetStateByName(String StateName){
		for (int i = 0; i < States.Count; i++)
		{
			State State = States[i];
			if (State.Name == StateName)
			{
				return State;
			}
		}
		return null;
	}
}
