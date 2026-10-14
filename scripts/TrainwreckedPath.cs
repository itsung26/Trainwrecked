using Godot;
using System;

/// <summary>
/// A <see cref="Path3D"/> whose queries use the curve that is baked right now.
/// </summary>
/// <remarks>
/// Baked offsets are distances along the loaded curve, not stable progress ratios.
/// Loading or unloading a chunk changes <see cref="Curve3D.GetBakedLength"/> and renumbers every ratio.
/// </remarks>
[GlobalClass]
public partial class TrainwreckedPath : Path3D
{
    /// <summary>
    /// Returns the world position at <paramref name="progressRatio"/> along the current baked curve.
    /// </summary>
    /// <param name="progressRatio">Fraction of the current baked length, from <c>0</c> at the start to <c>1</c> at the end.</param>
    /// <remarks>
    /// Adds a temporary <see cref="PathFollow3D"/> child and frees it at the end of the frame.
    /// </remarks>
    public Vector3 GetWorldPositionAtProgress(float progressRatio)
    {
        Vector3 ret;
        PathFollow3D Sampler = new PathFollow3D();
        AddChild(Sampler);
        Sampler.ProgressRatio = progressRatio;
        ret = Sampler.GlobalPosition;
        Sampler.QueueFree();
        return ret;
    }

    /// <summary>
    /// Returns the world point on the baked curve closest to <paramref name="worldPos"/>.
    /// </summary>
    /// <returns><paramref name="worldPos"/> when the curve has no baked length.</returns>
    public Vector3 GetClosestPathPoint(Vector3 worldPos)
    {
        Curve3D curve = Curve;
        if (curve == null || curve.PointCount == 0 || curve.GetBakedLength() <= 0f)
        {
            return worldPos;
        }

        Vector3 closestLocal = curve.GetClosestPoint(ToLocal(worldPos));
        return ToGlobal(closestLocal);
    }

    /// <summary>
    /// Returns the baked pose closest to <paramref name="worldPos"/>, in world space.
    /// </summary>
    /// <remarks>
    /// The basis -Z axis points along increasing offset, and curve tilt is applied.
    /// When the curve has no baked length, the basis is identity and the origin is <paramref name="worldPos"/>.
    /// </remarks>
    public Transform3D GetClosestPathTransform(Vector3 worldPos)
    {
        Curve3D curve = Curve;
        if (curve == null || curve.PointCount == 0 || curve.GetBakedLength() <= 0f)
        {
            return new Transform3D(Basis.Identity, worldPos);
        }

        float offset = curve.GetClosestOffset(ToLocal(worldPos));
        Transform3D localOnCurve = curve.SampleBakedWithRotation(offset, cubic: false, applyTilt: true);
        return GlobalTransform * localOnCurve;
    }

    /// <summary>
    /// Returns the baked arc length from the start of the curve to the point closest to <paramref name="worldPos"/>.
    /// </summary>
    /// <returns>
    /// <c>0</c> when the curve has no baked length. That is also the offset of a real point at the start of a baked curve.
    /// </returns>
    public float GetClosestOffset(Vector3 worldPos)
    {
        Curve3D curve = Curve;
        if (curve == null || curve.PointCount == 0 || curve.GetBakedLength() <= 0f)
        {
            return 0f;
        }

        return curve.GetClosestOffset(ToLocal(worldPos));
    }

    /// <summary>
    /// Returns the baked pose at <paramref name="offset"/>, in world space.
    /// </summary>
    /// <param name="offset">Arc length from the start of the currently baked curve.</param>
    /// <remarks>
    /// The basis -Z axis points along increasing offset, and curve tilt is applied.
    /// Offsets outside the loaded curve are clamped to the nearest end and are not wrapped.
    /// When the curve has no baked length, returns this path's <see cref="Node3D.GlobalTransform"/>.
    /// </remarks>
    public Transform3D GetPathTransformAtOffset(float offset)
    {
        Curve3D curve = Curve;
        if (curve == null || curve.PointCount == 0 || curve.GetBakedLength() <= 0f)
        {
            return GlobalTransform;
        }

        offset = Mathf.Clamp(offset, 0f, curve.GetBakedLength());
        Transform3D localOnCurve = curve.SampleBakedWithRotation(offset, cubic: false, applyTilt: true);
        return GlobalTransform * localOnCurve;
    }
}
