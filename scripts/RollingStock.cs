using System;
using Godot;

/// <summary>Represents a physical body that rides upon a railroad.</summary>
[GlobalClass]
public abstract partial class RollingStock : AnimatableBody3D
{
    [Export] public Marker3D FrontCoupler { get; set; }
    [Export] public Marker3D RearCoupler { get; set; }
    /// <summary>Mass used when a consist sums the chain. Subclasses assign the default.</summary>
    [Export] public float RollingMass { get; protected set; } = 1.0f;
    /// <summary>Whether this stock can propel itself. Locomotives set this to true.</summary>
    public bool HasPropulsion { get; protected set; }
    public RollingStock CoupledAhead { get; set; }
    public RollingStock CoupledBehind { get; set; }

}
