using Godot;
using Godot.Collections;
using System;

[Tool]
[GlobalClass]
public partial class TerrainGenerator : Node3D
{
	public enum LOD
	{
		// Represents full vertex resolution.
		Full = 0,
		// Represents half vertex resolution.
		Half = 1,
		// Represents quarter vertex resolution.
		Quarter = 2,
		// Represents a no-draw.
		Skipdraw = -1
	}

	/// <summary>
	/// The camera that perceives the chunks. Chunk coordinate is derived from the
	/// camera's position.
	/// </summary>
	[Export] public Camera3D Camera { get; set; }
	[Export] public Texture2D BaseHeightmap { get; set; }
	[Export] int FullLodResolution { get; set; }
	/// <summary>
	/// Length and width of each chunk.
	/// </summary>
	[Export] public float ChunkSize { get; set; }
	/// <summary>
	/// The max ring distance in chunks where chunks will still be drawn.
	/// </summary>
	[Export] public int MaxDrawDistance { get; set; }


	[ExportToolButton("Generate Terrain", Icon = "MeshInstance3D")]
	public Callable GenerateTerrainButton => Callable.From(GenerateTerrain);


    public override void _Ready()
    {

    }

	public void GenerateTerrain()
	{
		// Clear existing terrain first.
		foreach (Node child in GetChildren())
		{
			if (child is not null)
			{
				child.Free();
			}
		}
		GD.Print("Generating terrain...");
		int chunksGenerated = 0;
		while (chunksGenerated < GetChunkCount(MaxDrawDistance))
		{
			TerrainChunk newChunk = new TerrainChunk();
			AddChild
		}
	}

	/// <summary>
	/// Returns the total amount of chunks that would fill
	/// rings rings, including the center chunk.
	/// </summary>
	/// <returns></returns>
	private int GetChunkCount(int rings)
	{
		TerrainChunk newChunk = new TerrainChunk();
		AddChild
		// Chebyshev radius R fills a square of side (2R + 1).
		int side = (2 * rings) + 1;
		return side * side;
	}

}
