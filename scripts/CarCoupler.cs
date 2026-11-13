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
    [Export] private Marker3D _couplePoint;
    /// <summary>Hook mesh whose local rotation is animated between open and closed.</summary>
    [Export] private MeshInstance3D _hookMesh;
    private Vector3 _openHookRotation = Vector3.Zero;
    private Vector3 _closedHookRotation = new Vector3(0.0f, Mathf.DegToRad(104.3f), 0.0f);

    public override void _Ready()
    {
        base._Ready();

        if (Engine.IsEditorHint())
        {
            return;
        }

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

        if (!NetworkManager.IsServer())
        {
            return;
        }

        UpdateHookRotation(delta);
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal=true)]
    public override void Interact()
    {
        if (!NetworkManager.IsServer())
        {
            return;
        }

        CarParent.ConsistParent.Split(GetSplittingIndex());
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
    /// Returns the coupling contact marker in this coupler's parent local space.
    /// </summary>
    /// <remarks>
    /// Uses local transforms only, so it is valid while the car is outside the scene tree.
    /// Parent is expected to be the owning <see cref="RollingStock"/>.
    /// </remarks>
    public Vector3 GetCouplingPointLocalPosition()
    {
        return Transform * _couplePoint.Position;
    }

    /// <summary>
    /// Returns the <see cref="TrainConsist.Split"/> index for the joint this coupler represents.
    /// </summary>
    /// <returns>
    /// Front coupler of car <c>i</c> returns <c>i</c> (head front is <c>0</c>).
    /// Rear coupler of car <c>i</c> returns <c>i + 1</c> (tail rear is <see cref="TrainConsist.Cars"/>.Count).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no <see cref="CarParent"/>, no <see cref="RollingStock.ConsistParent"/>,
    /// the car is not in that consist, or this coupler is not assigned as front or rear.
    /// </exception>
    public int GetSplittingIndex()
    {
        if (CarParent is null)
        {
            throw new InvalidOperationException("Coupler has no RollingStock parent.");
        }

        TrainConsist consist = CarParent.ConsistParent;
        if (consist is null)
        {
            throw new InvalidOperationException("Car is not parented to a TrainConsist.");
        }

        int carIndex = consist.Cars.IndexOf(CarParent);
        if (carIndex < 0)
        {
            throw new InvalidOperationException("Car is not in its consist's Cars list.");
        }

        if (ReferenceEquals(CarParent.FrontCoupler, this))
        {
            return carIndex;
        }

        if (ReferenceEquals(CarParent.RearCoupler, this))
        {
            return carIndex + 1;
        }

        throw new InvalidOperationException("Coupler is not assigned as FrontCoupler or RearCoupler on its car.");
    }

    /// <summary>
    /// Eases <c>_hookMesh</c> rotation toward open or closed based on <see cref="Clamped"/>.
    /// </summary>
    private void UpdateHookRotation(double delta)
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
