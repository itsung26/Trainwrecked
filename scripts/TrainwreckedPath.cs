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
}
