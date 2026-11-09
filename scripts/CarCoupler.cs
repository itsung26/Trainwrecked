using Godot;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
[GlobalClass]
public partial class CarCoupler : Interactable
{
    [Export] public Marker3D ClampPoint { get; set; }
    [Export] public MeshInstance3D HookMesh { get; set; }
    public bool Clamped { get; private set; }

    private Vector3 _openHookRotation = Vector3.Zero;
    private Vector3 _closedHookRotation = new Vector3(0.0f, 104.3f, 0.0f);

    [Export] public float _clampPoseProgress { get; set; }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override string[] _GetConfigurationWarnings()
    {   
        Array<string> warnings = new Array<string>(base._GetConfigurationWarnings());

        if (ClampPoint is null)
        {
            warnings.Add("Clamp point must be set.");
        }

        return warnings.ToArray();
    }

    public override void Interact()
    {
        throw new NotImplementedException();
    }

    private static Vector3 LerpVectors(Vector3 a, Vector3 b, float weight)
    {
        return a.Lerp(b, weight);
    }

}
