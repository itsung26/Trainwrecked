using Godot;
using System;

/// <summary>
/// Named scene-tree node that a <see cref="StateMachine"/> can enter and exit.
/// </summary>
/// <remarks>
/// <see cref="OnEnter"/> and <see cref="OnExit"/> update <see cref="IsActive"/> and are the
/// extension points for subclasses. Equality, including <c>==</c> and <c>!=</c>, treats two
/// states as the same when they are the same instance or share a <see cref="Node.Name"/>.
/// A <see langword="null"/> operand compares unequal to any instance, and two null operands compare equal.
/// </remarks>
[GlobalClass]
public partial class State : Node, IEquatable<State>
{
    /// <summary>
    /// <see langword="true"/> after <see cref="OnEnter"/> and <see langword="false"/> after <see cref="OnExit"/>.
    /// </summary>
    public bool IsActive = false;

    /// <summary>
    /// <see langword="true"/> when <see cref="StateMachine.EnterState(State)"/> may exit and enter
    /// this state again while it is already <see cref="StateMachine.CurrentState"/>.
    /// </summary>
    /// <remarks>
    /// When <see langword="false"/>, a request to enter the already current state returns
    /// before <see cref="OnEnter"/> or <see cref="OnExit"/> runs.
    /// </remarks>
    [Export] public bool CanBeReentered = false;

    /// <summary>
    /// Creates a state and assigns <see cref="Node.Name"/>.
    /// </summary>
    /// <param name="name">Node name used for lookup and equality.</param>
    public State(string name)
    {
        this.Name = name;
    }

    public State()
    {
    }

    /// <summary>
    /// Marks this state active. Overrides should call this base implementation to set <see cref="IsActive"/>.
    /// </summary>
    public virtual void OnEnter()
    {
        IsActive = true;
    }

    /// <summary>
    /// Marks this state inactive. Overrides should call this base implementation to clear <see cref="IsActive"/>.
    /// </summary>
    public virtual void OnExit()
    {
        IsActive = false;
    }

    /// <summary>
    /// Returns the debug instance string for this state.
    /// </summary>
    /// <returns>The string produced by <see cref="Debug.GenerateInstanceToString"/>.</returns>
    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
    }

    /// <summary>
    /// Returns whether <paramref name="obj"/> is a <see cref="State"/> equal to this instance.
    /// </summary>
    /// <param name="obj">Object to compare. A non-state or <see langword="null"/> compares unequal.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is a <see cref="State"/> that passes
    /// <see cref="Equals(State)"/>; otherwise <see langword="false"/>.
    /// </returns>
    public override bool Equals(object obj)
    {
        return Equals(obj as State);
    }

    /// <summary>
    /// Returns whether <paramref name="other"/> is this instance or has the same <see cref="Node.Name"/>.
    /// </summary>
    /// <param name="other">State to compare. May be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="other"/> is <see langword="null"/>;
    /// otherwise <see langword="true"/> when both refer to the same instance or share a name.
    /// </returns>
    public bool Equals(State other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Name == other.Name;
    }

    /// <summary>
    /// Returns a hash code derived from <see cref="Node.Name"/>.
    /// </summary>
    /// <returns>The name's hash code, or <c>0</c> when <see cref="Node.Name"/> is <see langword="null"/>.</returns>
    public override int GetHashCode()
    {
        return Name?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Returns whether <paramref name="a"/> and <paramref name="b"/> are the same instance or share a <see cref="Node.Name"/>.
    /// </summary>
    /// <param name="a">Left operand. May be <see langword="null"/>.</param>
    /// <param name="b">Right operand. May be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when both are the same instance (including both <see langword="null"/>)
    /// or both are non-null and have the same name; otherwise <see langword="false"/>.
    /// </returns>
    public static bool operator ==(State a, State b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Name == b.Name;
    }

    /// <summary>
    /// Returns whether <paramref name="a"/> and <paramref name="b"/> fail <see cref="op_Equality"/>.
    /// </summary>
    /// <param name="a">Left operand. May be <see langword="null"/>.</param>
    /// <param name="b">Right operand. May be <see langword="null"/>.</param>
    /// <returns>The negation of <see cref="op_Equality"/> for the same operands.</returns>
    public static bool operator !=(State a, State b)
    {
        return !(a == b);
    }
}
