using Godot;
using Godot.Collections;
using System;

[Tool]
[GlobalClass]
public partial class TerrainGenerator : Node3D
{
	/// <summary>
	/// The camera that perceives the chunks. Chunk coordinate is derived from the
	/// camera's position.
	/// </summary>
	[Export] public Camera3D Camera { get; set; }
	[Export] public Texture2D BaseHeightmap { get; set; }
	/// <summary>
	/// Length and width of each chunk.
	/// </summary>
	private float _chunkSize;
	[Export]
	public float ChunkSize
	{
		get => _chunkSize;
		set
		{
			if (Mathf.IsEqualApprox(_chunkSize, value))
			{
				return;
			}
			_chunkSize = value;
		}
	}
	/// <summary>
	/// The max ring distance in chunks where chunks will still be drawn.
	/// </summary>
	private int _maxDrawDistance;
	[Export]
	public int MaxDrawDistance
	{
		get => _maxDrawDistance;
		set
		{
			if (_maxDrawDistance == value)
			{
				return;
			}
			_maxDrawDistance = value;
		}
	}
	[ExportToolButton("Generate Terrain", Icon = "MeshInstance3D")]
	public Callable GenerateTerrainButton => Callable.From(GenerateTerrain);


    public override void _Ready()
    {
    }

	public void GenerateTerrain()
	{
		GD.Print("foo");
	}

}
