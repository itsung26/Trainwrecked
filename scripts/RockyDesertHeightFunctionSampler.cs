using Godot;
using System;

[Tool]
[GlobalClass, Icon("res://addons/at-icons/mesh/function.svg")]
public partial class RockyDesertHeightFunctionSampler : HeightFunctionSampler
{
    [Export] public FastNoiseLite DuneHeightNoise { get; set; }
    [Export] public override float HeightFunctionScale { get; set; } = 1.0f;
    // [Export] public float DuneSpacing { get; set; }
    // [Export] public float DipHeight { get; set; }
    // [Export] public float DuneHeight { get; set; }
    // [Export] public float Sharpness { get; set; }

	public override float Sample(Vector2 uv)
	{
        // uv.x corresponds to xyz.x
        // uv.y corresponds to xyz.z
        // Both are expected in world coordinates.

        return DuneHeightNoise is not null ? (DuneHeightNoise.GetNoise2Dv(uv) - 0.5f) * HeightFunctionScale : 0f;
    }

	public override float Sample(float u, float v)
	{
		return Sample(new Vector2(u, v));
	}
}
