using Godot;
using System;

[GlobalClass]
public partial class State : Node, IEquatable<State>
{
    public bool IsActive = false;
    [Export] public bool CanBeReentered = false;

    public State(string name)
    {
        this.Name = name;
    }

    public virtual void OnEnter()
    {
        IsActive = true;
    }

    public virtual void OnExit()
    {
        IsActive = false;
    }

    public override string ToString()
    {
        return Debug.GenerateInstanceToString(this);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as State);
    }

    public bool Equals(State other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name?.GetHashCode() ?? 0;
    }

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

    public static bool operator !=(State a, State b)
    {
        return !(a == b);
    }
}
