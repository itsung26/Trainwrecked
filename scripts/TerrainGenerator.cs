using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Generates and manages a grid of <see cref="TerrainChunk"/> children.
/// </summary>
/// <remarks>
/// This node's origin sits at the shared corner of four chunks; there is no center chunk.
/// Active chunks form an even grid spanning indices <c>[-rings, rings)</c> on X and Z,
/// so at <c>rings == 1</c> the four chunks <c>(-1,-1)</c>, <c>(-1,0)</c>, <c>(0,-1)</c>,
/// and <c>(0,0)</c> meet at the generator origin.
/// </remarks>
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
	/// <remarks>
	/// Chunk indices are relative to this node's origin (the corner where four chunks meet):
	/// <c>floor(localX / ChunkSize)</c> and <c>floor(localZ / ChunkSize)</c>.
	/// There is no center chunk; the camera always lies in one of the four quadrants
	/// adjacent to the generator origin when near it.
	/// </remarks>
	[Export] public Camera3D Camera { get; set; }
	[Export] public Texture2D BaseHeightmap { get; set; }
	[Export] public BaseMaterial3D TerrainMaterial { get; set; }
	[Export] int FullLodResolution { get; set; }
	/// <summary>
	/// Length and width of each chunk.
	/// </summary>
	/// <remarks>
	/// Each chunk node's <see cref="Node3D.Position"/> is the min corner of that chunk.
	/// The chunk occupies <c>[Position.X, Position.X + ChunkSize]</c> on X and
	/// <c>[Position.Z, Position.Z + ChunkSize]</c> on Z. Local placement for grid
	/// indices <c>(cx, cz)</c> is <c>(cx * ChunkSize, 0, cz * ChunkSize)</c>,
	/// with the generator origin at the corner shared by the four chunks around
	/// indices <c>(-1,-1)</c>, <c>(-1,0)</c>, <c>(0,-1)</c>, and <c>(0,0)</c>.
	/// </remarks>
	[Export] public float ChunkSize { get; set; }
	/// <summary>
	/// LOD applied at each ring distance from the generator origin.
	/// </summary>
	/// <remarks>
	/// Index <c>i</c> is ring <c>i + 1</c> (the innermost four chunks are ring 1 at index 0).
	/// <see cref="Array.Count"/> is the max draw distance in rings: a length of <c>R</c>
	/// yields a <c>2R × 2R</c> grid (no center chunk). Use <see cref="LOD.Skipdraw"/> to
	/// omit a ring. See <see cref="GetLodForRing"/>.
	/// </remarks>
	[Export] public Array<LOD> RingLods { get; set; } = new Array<LOD>();
	/// <summary>
	/// Multiplier applied to sampled heightmap values (typically 0–1) when displacing mesh vertices.
	/// </summary>
	[Export] public float HeightScale { get; set; } = 64f;

	public Image BaseHeightmapImage { get; private set; }

	[ExportToolButton("Generate Terrain", Icon = "MeshInstance3D")]
	public Callable GenerateTerrainButton => Callable.From(GenerateTerrain);


    public override void _Ready()
    {
		RefreshHeightmapImage();
    }

	/// <summary>
	/// Rebuilds <see cref="BaseHeightmapImage"/> from <see cref="BaseHeightmap"/>.
	/// </summary>
	public void RefreshHeightmapImage()
	{
		if (BaseHeightmap is null)
		{
			BaseHeightmapImage = null;
			return;
		}

		BaseHeightmapImage = BaseHeightmap.GetImage();
		if (BaseHeightmapImage is not null && BaseHeightmapImage.IsCompressed())
		{
			BaseHeightmapImage.Decompress();
		}
	}

	/// <summary>
	/// Returns a heightmap value for a normalized sampling coord uv. Sample filtering is
	/// interpolated. Sampling past 1.0 bounds returns repeating values.
	/// </summary>
	/// <param name="uv"></param>
	/// <returns></returns>
	public float SampleHeightmap(Vector2 uv)
	{
		if (BaseHeightmapImage is null)
		{
			return 0f;
		}

		int width = BaseHeightmapImage.GetWidth();
		int height = BaseHeightmapImage.GetHeight();
		if (width <= 0 || height <= 0)
		{
			return 0f;
		}

		// Repeat: wrap UV into [0, 1).
		float u = uv.X - Mathf.Floor(uv.X);
		float v = uv.Y - Mathf.Floor(uv.Y);

		// Continuous pixel space; bilinear across wrapped texel neighbors.
		float x = u * width - 0.5f;
		float y = v * height - 0.5f;

		int x0 = Mathf.FloorToInt(x);
		int y0 = Mathf.FloorToInt(y);
		float tx = x - x0;
		float ty = y - y0;

		int x0w = WrapIndex(x0, width);
		int x1w = WrapIndex(x0 + 1, width);
		int y0w = WrapIndex(y0, height);
		int y1w = WrapIndex(y0 + 1, height);

		float h00 = BaseHeightmapImage.GetPixel(x0w, y0w).R;
		float h10 = BaseHeightmapImage.GetPixel(x1w, y0w).R;
		float h01 = BaseHeightmapImage.GetPixel(x0w, y1w).R;
		float h11 = BaseHeightmapImage.GetPixel(x1w, y1w).R;

		float h0 = Mathf.Lerp(h00, h10, tx);
		float h1 = Mathf.Lerp(h01, h11, tx);
		return Mathf.Lerp(h0, h1, ty);
	}

	private int WrapIndex(int index, int size)
	{
		return ((index % size) + size) % size;
	}

	public void GenerateTerrain()
	{
		RefreshHeightmapImage();

		// Clear existing terrain first.
		foreach (Node child in GetChildren())
		{
			if (child is not null)
			{
				child.Free();
			}
		}

		// Single test chunk at grid (0, 0) — min corner at the generator origin.
		TerrainChunk testChunk = new TerrainChunk();
		AddChild(testChunk);
		testChunk.Position = Vector3.Zero;
		testChunk.Generate();
		testChunk.GetChild<MeshInstance3D>(0).MaterialOverride = TerrainMaterial;
	}

	/// <summary>
	/// Returns the <see cref="LOD"/> configured for the given 1-based ring distance.
	/// </summary>
	/// <param name="ring">Ring distance; <c>1</c> is the innermost four chunks.</param>
	/// <returns>
	/// The mapped LOD, or <see cref="LOD.Skipdraw"/> if <paramref name="ring"/> is
	/// out of range or missing from <see cref="RingLods"/>.
	/// </returns>
	public LOD GetLodForRing(int ring)
	{
		if (RingLods is null || ring < 1 || ring > RingLods.Count)
		{
			return LOD.Skipdraw;
		}
		return RingLods[ring - 1];
	}

	/// <summary>
	/// Returns the total amount of chunks that would fill
	/// <paramref name="rings"/> rings around the generator origin.
	/// </summary>
	/// <remarks>
	/// Because the generator origin is the corner of four chunks (no center chunk),
	/// radius <c>R</c> fills an even square of side <c>2R</c> spanning indices
	/// <c>[-R, R)</c> on X and Z.
	/// </remarks>
	/// <returns></returns>
	private int GetChunkCount(int rings)
	{
		// Even grid: radius R fills a square of side 2R (four chunks meet at the origin).
		int side = 2 * rings;
		return side * side;
	}

	public int GetResolutionByLod(TerrainGenerator.LOD lod)
	{
		switch (lod)
		{
			case LOD.Full:
				return FullLodResolution;
			
			case LOD.Half:
				return FullLodResolution / 2;

			case LOD.Quarter:
				return FullLodResolution / 4;
			
			case LOD.Skipdraw:
				return 0;
			
			default:
				return 0;
		}
	}

	/// <summary>
	/// Returns the chunk grid indices for a horizontal world-space position.
	/// </summary>
	/// <param name="pos">
	/// World X in <c>X</c>, world Z in <c>Y</c>. World up is Y and is not part of this vector;
	/// the second component is Z, not vertical.
	/// </param>
	public Vector2I GetChunkCoordinatesFromWorldCoordinates(Vector2 pos)
	{
		return GetChunkCoordinatesFromWorldCoordinates(new Vector3(pos.X, 0f, pos.Y));
	}

	/// <summary>
	/// Returns the chunk grid indices for a world-space position.
	/// </summary>
	/// <remarks>
	/// Converts <paramref name="pos"/> into this node's local space, then uses
	/// <c>floor(localX / ChunkSize)</c> and <c>floor(localZ / ChunkSize)</c>.
	/// Local Y (up) is ignored.
	/// </remarks>
	public Vector2I GetChunkCoordinatesFromWorldCoordinates(Vector3 pos)
	{
		if (ChunkSize == 0f)
		{
			return Vector2I.Zero;
		}

		Vector3 local = ToLocal(pos);
		int cx = Mathf.FloorToInt(local.X / ChunkSize);
		int cz = Mathf.FloorToInt(local.Z / ChunkSize);
		return new Vector2I(cx, cz);
	}

}
