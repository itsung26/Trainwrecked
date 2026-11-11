using Godot;
using System;
using Godot.Collections;
using System.Linq;

/// <summary>
/// Interactable train coupler with a hook mesh that opens and closes when clamped.
/// </summary>
/// <remarks>
/// Intended to be instanced from <c>car_coupler.tscn</c> and assigned to
/// <see cref="RollingStock.FrontCoupler"/> / <see cref="RollingStock.RearCoupler"/>.
/// Consist spacing uses <see cref="GetCouplingPointPosition"/> as the arc sample point.
/// <see cref="Clamped"/> should be driven by consist join/split logic so mating ends stay in sync.
/// </remarks>
[Tool]
[GlobalClass]
public partial class CarCoupler : Interactable
{
    /// <summary>Radians per second used when rotating the hook toward open or closed.</summary>
    private const float HookRotateSpeed = 8.0f;

    /// <summary>
    /// Whether this coupler is locked closed against a mate.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the hook eases toward the closed pose; when
    /// <see langword="false"/>, toward the open pose. Visual only until gameplay sets it from
    /// <see cref="TrainConsist"/> join/split.
    /// </remarks>
    public bool Clamped { get; set; }
    public RollingStock CarParent => GetParent() as RollingStock;

    [ExportGroup("Editor Configuration")]
    /// <summary>Marker at the coupling contact point along the car local <c>-Z</c> axis.</summary>
    [Export] private Marker3D _couplePoint;
    /// <summary>Hook mesh whose local rotation is animated between open and closed.</summary>
    [Export] private MeshInstance3D _hookMesh;
    private Vector3 _openHookRotation = Vector3.Zero;
    private Vector3 _closedHookRotation = new Vector3(0.0f, Mathf.DegToRad(104.3f), 0.0f);

    public override void _Ready()
    {
        base._Ready();
    }

    /// <summary>
    /// Advances the hook rotation toward the pose implied by <see cref="Clamped"/>.
    /// </summary>
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Engine.IsEditorHint())
        {
            return;
        }

        RotateHookIfClamped(delta);
    }

    /// <summary>
    /// Returns editor warnings for a missing or misaligned couple point, or a bare GlobalClass instance.
    /// </summary>
    public override string[] _GetConfigurationWarnings()
    {   
        Array<string> warnings = new Array<string>(base._GetConfigurationWarnings());

        if (_couplePoint is null)
        {
            warnings.Add("Couple point must be set.");
        }
        else if (_couplePoint.Position.X != 0.0f || _couplePoint.Position.Y != 0.0f)
        {
            warnings.Add("Misaligned coupler detected. Ensure all non-z axis are set to 0.");
        }

        // Empty when added as a bare GlobalClass node; set when this is an instanced .tscn root.
        if (string.IsNullOrEmpty(SceneFilePath))
        {
            warnings.Add("Coupler not instantiated as scene. Instance car_coupler.tscn instead of adding CarCoupler directly.");
        }

        return warnings.ToArray();
    }

    /// <summary>
    /// Handles player interact on this coupler.
    /// </summary>
    /// <remarks>
    /// Currently logs only. Coupling should ultimately go through <see cref="TrainConsist"/> join/split
    /// so both mating <see cref="Clamped"/> flags update together. RPC is AnyPeer with CallLocal.
    /// </remarks>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal=true)]
    public override void Interact()
    {
        Debug.Log($"Interacted with {this}");
    }

    /// <summary>
    /// Returns the world-space position of the coupling contact marker.
    /// </summary>
    /// <returns>Global position of <c>_couplePoint</c>.</returns>
    /// <remarks>
    /// Used by <see cref="TrainConsist"/> arc spacing. Requires <c>_couplePoint</c> to be assigned.
    /// </remarks>
    public Vector3 GetCouplingPointPosition()
    {
        return _couplePoint.GlobalPosition;
    }

    /// <summary>
    /// Eases <c>_hookMesh</c> rotation toward open or closed based on <see cref="Clamped"/>.
    /// </summary>
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
    /// <param name="from">Current Euler rotation in radians.</param>
    /// <param name="to">Target Euler rotation in radians.</param>
    /// <param name="delta">Maximum radians to move on each axis this step.</param>
    /// <returns>The updated Euler angles.</returns>
    private static Vector3 MoveTowardEuler(Vector3 from, Vector3 to, float delta)
    {
        return new Vector3(
            Mathf.RotateToward(from.X, to.X, delta),
            Mathf.RotateToward(from.Y, to.Y, delta),
            Mathf.RotateToward(from.Z, to.Z, delta)
        );
    }

}
