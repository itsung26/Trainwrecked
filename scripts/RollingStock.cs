using Godot;
using System;

[GlobalClass]
public abstract partial class RollingStock : AnimatableBody3D
{
    [Export] public bool CanCoupleFront { get; set; } = true;
    [Export] public bool CanCoupleBack { get; set; } = true;
    [Export] public float RollingMass { get; set; } = 1.0f;
    [Export] public float VerticalOffset { get; set; }
    public RollingStock CoupledAhead { get; private set; }
    public RollingStock CoupledBehind { get; private set; }
}
