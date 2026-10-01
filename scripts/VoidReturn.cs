using Godot;
using System;

/// <summary>
/// An <see cref="Area3D"/> placed beneath the playable map that catches bodies
/// which fall out of the world and returns them to a safe position above.
/// </summary>
/// <remarks>
/// Intended to sit under the terrain with a large world-boundary collision shape.
/// Bodies that enter this volume are teleported (or otherwise relocated) back
/// into the level so they are not lost below the map.
/// </remarks>
public partial class VoidReturn : Area3D
{
}
