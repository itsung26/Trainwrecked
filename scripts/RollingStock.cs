using System;
using Godot;

/// <summary>Represents a physical body that rides upon a railroad.</summary>
[GlobalClass, Icon("res://assets/2d assets/rolling_stock_icon.svg")]
public abstract partial class RollingStock : AnimatableBody3D
{
    [Export] public CarCoupler FrontCoupler { get; set; }
    [Export] public CarCoupler RearCoupler { get; set; }
    /// <summary>Mass used when a consist sums the chain. Subclasses assign the default.</summary>
    [Export] public float RollingMass { get; protected set; } = 1.0f;
    /// <summary>Whether this stock can propel itself. Locomotives set this to true.</summary>
    public bool HasPropulsion { get; protected set; }
    /// <summary>Baked arc length of this car's origin on its consist's track.</summary>
    public float TrackOffset { get; set; }
    public RollingStock CoupledAhead
    {
        get => _coupledAhead;
        set => SetCoupledAhead(value);
    }
    public RollingStock CoupledBehind
    {
        get => _coupledBehind;
        set => SetCoupledBehind(value);
    }

    private RollingStock _coupledAhead;
    private RollingStock _coupledBehind;

    /// <summary>Stub: wire ahead coupling (clear neighbors, update Head/Tail, etc.).</summary>
    protected virtual void SetCoupledAhead(RollingStock value)
    {
        _coupledAhead = value;

        if (FrontCoupler is not null)
        {
            FrontCoupler.Clamped = value is not null;
        }
    }

    /// <summary>Stub: wire behind coupling (clear neighbors, update Head/Tail, etc.).</summary>
    protected virtual void SetCoupledBehind(RollingStock value)
    {
        _coupledBehind = value;

        if (RearCoupler is not null)
        {
            RearCoupler.Clamped = value is not null;
        }
    }
}
