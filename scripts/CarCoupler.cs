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
    [Export] public float HookRotateSpeed { get; set; }

    public bool Clamped { get; private set; }

    private Vector3 _openHookRotation = Vector3.Zero;
    private Vector3 _closedHookRotation = new Vector3(0.0f, Mathf.DegToRad(104.3f), 0.0f);

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        RotateHookIfClamped(delta);
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal=true)]
    public override void Interact()
    {
        Debug.Log($"Interacted with {this}");
    }

    private void RotateHookIfClamped(double delta)
    {
        if (HookMesh is null)
        {
            return;
        }

        float step = (float)delta * HookRotateSpeed;
        Vector3 target = Clamped ? _closedHookRotation : _openHookRotation;
        HookMesh.Rotation = MoveTowardEuler(HookMesh.Rotation, target, step);
    }

    /// <summary>
    /// Moves each Euler axis toward <paramref name="to"/> along the shortest arc.
    /// </summary>
    private static Vector3 MoveTowardEuler(Vector3 from, Vector3 to, float delta)
    {
        return new Vector3(
            Mathf.RotateToward(from.X, to.X, delta),
            Mathf.RotateToward(from.Y, to.Y, delta),
            Mathf.RotateToward(from.Z, to.Z, delta)
        );
    }

}
