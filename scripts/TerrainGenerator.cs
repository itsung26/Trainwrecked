using Godot;
using System;

[GlobalClass]
public partial class TerrainGenerator : Node3D
{
	/// <summary>
	/// The camera that perceives the chunks. Chunk coordinate is derived from the
	/// camera's position.
	/// </summary>
	[Export] public Camera3D Camera { get; set; }


}
