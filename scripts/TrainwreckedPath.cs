using Godot;
using System;

[GlobalClass]
public partial class TrainwreckedPath : Path3D
{
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

    /// Returns the point on the currently baked curve closest to <paramref name="worldPos"/>.
    /// Searches world space rather than <c>progressRatio</c>, which shifts when chunks load or unload.
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

    /// Returns the closest baked pose in world space. The basis -Z axis points along increasing offset.
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
}
