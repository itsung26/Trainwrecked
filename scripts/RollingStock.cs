using System;
using Godot;

/// <summary>Represents a physical body that rides upon a railroad.</summary>
[Tool, GlobalClass, Icon("res://assets/2d assets/rolling_stock_icon.svg")]
public abstract partial class RollingStock : AnimatableBody3D
{
    [Export] public CarCoupler FrontCoupler { get; set; }
    [Export] public CarCoupler RearCoupler { get; set; }
    /// <summary>Mass used when a consist sums the chain. Subclasses assign the default.</summary>
    [Export] public float RollingMass { get; protected set; } = 1.0f;
    /// <summary>Whether this stock can propel itself. Locomotives set this to true.</summary>
    public bool HasPropulsion { get; protected set; }
    public RollingStock Ahead
    {
        get => _ahead;
        set => SetAhead(value);
    }
    public RollingStock Behind
    {
        get => _behind;
        set => SetBehind(value);
    }

    private RollingStock _ahead;
    private RollingStock _behind;

    /// <summary>Stub: wire ahead coupling (clear neighbors, update Head/Tail, etc.).</summary>
    protected virtual void SetAhead(RollingStock value)
    {
        _ahead = value;

        if (FrontCoupler is not null)
        {
            FrontCoupler.Clamped = value is not null;
        }
    }

    /// <summary>Stub: wire behind coupling (clear neighbors, update Head/Tail, etc.).</summary>
    protected virtual void SetBehind(RollingStock value)
    {
        _behind = value;

        if (RearCoupler is not null)
        {
            RearCoupler.Clamped = value is not null;
        }
    }

    /// Returns the local z axis distance between the two coupler coupling points.
    public float CouplerSpan()
    {
        if (FrontCoupler is null || RearCoupler is null)
        {
            throw new InvalidOperationException();
        }

        float frontZ = ToLocal(FrontCoupler.GetCouplingPointPosition()).Z;
        float rearZ = ToLocal(RearCoupler.GetCouplingPointPosition()).Z;
        return Mathf.Abs(frontZ - rearZ);
    }

    /// Returns the local z axis distance from the origin to the front coupler coupling point.
    public float FrontCouplerDistance()
    {
        return Mathf.Abs(ToLocal(FrontCoupler.GetCouplingPointPosition()).Z);
    }

    public float RearCouplerDistance()
    {
        return Mathf.Abs(ToLocal(RearCoupler.GetCouplingPointPosition()).Z);
    }
}
