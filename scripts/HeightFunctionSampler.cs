using Godot;
using System;

/// <summary>
/// Abstract <see cref="Resource"/> that evaluates a height at a 2D sample coordinate.
/// </summary>
/// <remarks>
/// Intended for terrain generation via <see cref="TerrainGenerator.HeightFunction"/>,
/// but callers may sample it for any purpose. Concrete subclasses define the height
/// function. Sampling coordinates are typically UV-like; exact domain and wrapping
/// are defined by each implementation.
/// <para/>
/// Marked <c>[Tool]</c> so instances can be assigned on tool scripts such as
/// <see cref="TerrainGenerator"/> in the editor without cast failures.
/// </remarks>
[Tool]
[GlobalClass, Icon("res://addons/at-icons/mesh/function.svg")]
public abstract partial class HeightFunctionSampler : Resource
{
	/// <summary>
	/// Multiplier applied to the sampled height by concrete implementations.
	/// </summary>
	public abstract float HeightFunctionScale { get; set; }

	/// <summary>
	/// Returns the height at the given 2D sample coordinate.
	/// </summary>
	/// <param name="uv">
	/// Sample position (typically a UV-like <c>(u, v)</c> pair; meaning is
	/// defined by the concrete sampler).
	/// </param>
	/// <returns>Height at <paramref name="uv"/>.</returns>
	public abstract float Sample(Vector2 uv);

	/// <summary>
	/// Returns the height at the given <paramref name="u"/> / <paramref name="v"/>
	/// sample coordinates.
	/// </summary>
	/// <param name="u">First sample axis (often horizontal / X-aligned UV).</param>
	/// <param name="v">Second sample axis (often depth / Z-aligned UV).</param>
	/// <returns>Height at <c>(u, v)</c>.</returns>
	public abstract float Sample(float u, float v);
}
