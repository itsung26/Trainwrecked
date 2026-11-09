using Godot;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
[GlobalClass]
public partial class CarCoupler : Interactable
{
    [Export] public float HookRotateSpeed { get; set; } = 8.0f;
    public bool Clamped { get; set; }

    [ExportGroup("Editor Configuration")]
    [Export] private Marker3D _couplePoint;
    [Export] private MeshInstance3D _hookMesh;
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

        if (_couplePoint is null)
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

    public Vector3 GetCouplingPointPosition()
    {
        return _couplePoint.GlobalPosition;
    }

    private void RotateHookIfClamped(double delta)
    {
        if (_hookMesh is null)
        {
            return;
        }

        float step = (float)delta * HookRotateSpeed;
        Vector3 target = Clamped ? _closedHookRotation : _openHookRotation;
        _hookMesh.Rotation = MoveTowardEuler(_hookMesh.Rotation, target, step);
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
