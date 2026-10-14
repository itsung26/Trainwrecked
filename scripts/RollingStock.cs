using System;
using Godot;

/// Represents a physical body that rides upon a railroad.
[GlobalClass]
public abstract partial class RollingStock : AnimatableBody3D
{
    [Export] public bool CanCoupleFront { get; set; } = true;
    [Export] public bool CanCoupleRear { get; set; } = true;
    [Export] public Marker3D FrontCoupler { get; set; }
    [Export] public Marker3D RearCoupler { get; set; }
    [Export] public float RollingMass { get; set; } = 1.0f;
    [Export] public float VerticalOffset { get; set; }
    public RollingStock CoupledAhead { get; private set; }
    public RollingStock CoupledBehind { get; private set; }
}
