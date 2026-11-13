using System;
using Godot;

/// <summary>
/// Abstract physical body that rides a railroad as part of a <see cref="TrainConsist"/>.
/// </summary>
/// <remarks>
/// Child order in the consist defines the chain: <see cref="Ahead"/> is toward the head,
/// <see cref="Behind"/> toward the tail. Coupler spacing along the track uses
/// <see cref="FrontCouplerDistance"/> / <see cref="RearCouplerDistance"/>.
/// </remarks>
[Tool, GlobalClass, Icon("res://assets/2d assets/rolling_stock_icon.svg")]
public abstract partial class RollingStock : AnimatableBody3D
{
    /// <summary>Coupler on the forward end of this car (local <c>-Z</c> toward the head).</summary>
    [Export] public CarCoupler FrontCoupler { get; set; }

    /// <summary>Coupler on the aft end of this car (local <c>+Z</c> toward the tail).</summary>
    [Export] public CarCoupler RearCoupler { get; set; }

    /// <summary>Mass used when a consist sums the chain. Subclasses assign the default.</summary>
    [Export] public float RollingMass { get; protected set; } = 1.0f;

    /// <summary>
    /// Whether this stock can propel itself.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> for locomotives; <see langword="false"/> for unpowered cars.
    /// </remarks>
    public bool HasPropulsion { get; protected set; }

    /// <summary>
    /// Owning <see cref="TrainConsist"/>, or <see langword="null"/> if the parent is not one.
    /// </summary>
    /// <remarks>
    /// Resolved each access via <see cref="Node.GetParent"/>. Cars are expected to be
    /// direct children of a <see cref="TrainConsist"/> while in a consist.
    /// </remarks>
    public TrainConsist ConsistParent => GetParent() as TrainConsist;

    /// <summary>
    /// Neighbor toward the head of the consist, or <see langword="null"/> if this car is the head.
    /// </summary>
    /// <remarks>
    /// Setting this updates <see cref="FrontCoupler"/>.<see cref="CarCoupler.Clamped"/>:
    /// clamped when a neighbor is present, open when cleared.
    /// </remarks>
    public RollingStock Ahead
    {
        get => _ahead;
        set => SetAhead(value);
    }

    /// <summary>
    /// Neighbor toward the tail of the consist, or <see langword="null"/> if this car is the tail.
    /// </summary>
    /// <remarks>
    /// Setting this updates <see cref="RearCoupler"/>.<see cref="CarCoupler.Clamped"/>:
    /// clamped when a neighbor is present, open when cleared.
    /// </remarks>
    public RollingStock Behind
    {
        get => _behind;
        set => SetBehind(value);
    }

    private RollingStock _ahead;
    private RollingStock _behind;

    /// <summary>
    /// Assigns the ahead neighbor and syncs the front coupler clamp state.
    /// </summary>
    /// <param name="value">
    /// The car ahead, or <see langword="null"/> to clear the link and open the front coupler.
    /// </param>
    protected virtual void SetAhead(RollingStock value)
    {
        _ahead = value;

        if (FrontCoupler is not null)
        {
            FrontCoupler.Clamped = value is not null;
        }
    }

    /// <summary>
    /// Assigns the behind neighbor and syncs the rear coupler clamp state.
    /// </summary>
    /// <param name="value">
    /// The car behind, or <see langword="null"/> to clear the link and open the rear coupler.
    /// </param>
    protected virtual void SetBehind(RollingStock value)
    {
        _behind = value;

        if (RearCoupler is not null)
        {
            RearCoupler.Clamped = value is not null;
        }
    }

    /// <summary>
    /// Returns the local Z distance between the front and rear coupling points.
    /// </summary>
    /// <returns>
    /// <see cref="FrontCouplerDistance"/> plus <see cref="RearCouplerDistance"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="FrontCoupler"/> or <see cref="RearCoupler"/> is <see langword="null"/>.
    /// </exception>
    public float CouplerSpan()
    {
        if (FrontCoupler is null || RearCoupler is null)
        {
            throw new InvalidOperationException("Neither coupler can be null.");
        }

        return FrontCouplerDistance() + RearCouplerDistance();
    }

    /// <summary>
    /// Returns the local Z distance from this car's origin to the front coupling point.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="FrontCoupler"/> is <see langword="null"/>.
    /// </exception>
    public float FrontCouplerDistance()
    {
        if (FrontCoupler is null)
        {
            throw new InvalidOperationException("Front coupler cannot be null.");
        }

        return Mathf.Abs(FrontCoupler.GetCouplingPointLocalPosition().Z);
    }

    /// <summary>
    /// Returns the local Z distance from this car's origin to the rear coupling point.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="RearCoupler"/> is <see langword="null"/>.
    /// </exception>
    public float RearCouplerDistance()
    {
        if (RearCoupler is null)
        {
            throw new InvalidOperationException("Rear coupler cannot be null.");
        }

        return Mathf.Abs(RearCoupler.GetCouplingPointLocalPosition().Z);
    }
}
