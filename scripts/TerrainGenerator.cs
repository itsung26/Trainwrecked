using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// Generates and manages a grid of <see cref="TerrainChunk"/> children.
/// </summary>
/// <remarks>
/// Chunks extend in +X and -Z from their origins. Active chunks stream around the
/// <see cref="Camera"/>: each frame (when the camera enters a new chunk cell) the
/// generator keeps chunks whose ring distance is within <see cref="RingLods"/> and
/// rebuilds them when their ring LOD changes. Chunks are created and freed directly
/// (no pool).
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
	/// Chunk indices are relative to this node's origin (the corner where four chunks meet).
	/// X uses <c>floor(localX / ChunkSize)</c>; Z uses <c>ceil(localZ / ChunkSize)</c>
	/// because each chunk origin is the max-Z corner of its cell.
	/// </remarks>
	[Export] public Camera3D Camera { get; set; }
	[Export] public Texture2D BaseHeightmap { get; set; }
	[Export] public BaseMaterial3D TerrainMaterial { get; set; }
	[Export] int FullLodResolution { get; set; }
	/// <summary>
	/// Length and width of each chunk.
	/// </summary>
	/// <remarks>
	/// Each chunk node's <see cref="Node3D.Position"/> is the corner origin of that chunk
	/// (min X, max Z). The chunk occupies <c>[Position.X, Position.X + ChunkSize]</c> on X and
	/// <c>[Position.Z - ChunkSize, Position.Z]</c> on Z. Local placement for grid
	/// indices <c>(cx, cz)</c> is <c>(cx * ChunkSize, 0, cz * ChunkSize)</c>.
	/// </remarks>
	[Export] public float ChunkSize { get; set; }
	/// <summary>
	/// LOD applied at each ring distance from the camera's chunk.
	/// </summary>
	/// <remarks>
	/// Index <c>i</c> is ring <c>i + 1</c>. <see cref="Array.Count"/> is the max draw
	/// distance in rings. Use <see cref="LOD.Skipdraw"/> to omit a ring.
	/// See <see cref="GetLodForRing"/>.
	/// </remarks>
	[Export] public Array<LOD> RingLods { get; set; } = new Array<LOD>();
	/// <summary>
	/// Multiplier applied to sampled heightmap values (typically 0–1) when displacing mesh vertices.
	/// </summary>
	[Export] public float HeightScale { get; set; } = 64f;

	public Image BaseHeightmapImage { get; private set; }

	private readonly System.Collections.Generic.Dictionary<Vector2I, TerrainChunk> _activeChunks = new System.Collections.Generic.Dictionary<Vector2I, TerrainChunk>();
	private Vector2I _lastCameraChunk;
	private bool _hasLastCameraChunk;

	[ExportToolButton("Generate Terrain", Icon = "MeshInstance3D")]
	public Callable GenerateTerrainButton => Callable.From(GenerateTerrain);


	public override void _Ready()
	{
		RefreshHeightmapImage();

		if (Engine.IsEditorHint())
		{
			return;
		}

		// Defer first stream so NoiseTexture2D / Camera are ready to sample.
		Callable.From(() => UpdateChunks(force: true)).CallDeferred();
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		UpdateChunks();
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
	/// Ensures <see cref="BaseHeightmapImage"/> is loaded. Returns
	/// <see langword="true"/> if the image became available this call.
	/// </summary>
	private bool TryAcquireHeightmapImage()
	{
		if (BaseHeightmap is null)
		{
			return false;
		}

		if (BaseHeightmapImage is not null)
		{
			return false;
		}

		RefreshHeightmapImage();
		return BaseHeightmapImage is not null;
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

	/// <summary>
	/// Clears active chunks, refreshes the heightmap cache, and force-streams around the camera.
	/// </summary>
	public void GenerateTerrain()
	{
		RefreshHeightmapImage();
		ClearActiveChunks();
		_hasLastCameraChunk = false;
		UpdateChunks(force: true);
	}

	/// <summary>
	/// Syncs active chunks to the camera: spawn/free by ring, rebuild when LOD changes.
	/// </summary>
	/// <param name="force">When <see langword="true"/>, runs even if the camera chunk is unchanged.</param>
	private void UpdateChunks(bool force = false)
	{
		if (Camera is null || ChunkSize == 0f || RingLods is null || RingLods.Count == 0)
		{
			return;
		}

		// NoiseTexture2D.GetImage() is often unavailable during _Ready; retry until loaded.
		bool heightmapJustReady = TryAcquireHeightmapImage();
		if (heightmapJustReady)
		{
			RebuildAllActiveChunkMeshes();
		}

		Vector2I cameraChunk = GetChunkCoordinatesFromWorldCoordinates(Camera.GlobalPosition);
		if (!force && !heightmapJustReady && _hasLastCameraChunk && cameraChunk == _lastCameraChunk)
		{
			return;
		}

		_lastCameraChunk = cameraChunk;
		_hasLastCameraChunk = true;

		int maxRing = RingLods.Count;
		HashSet<Vector2I> desired = new HashSet<Vector2I>();

		for (int dz = -maxRing; dz <= maxRing; dz++)
		{
			for (int dx = -maxRing; dx <= maxRing; dx++)
			{
				Vector2I coord = new Vector2I(cameraChunk.X + dx, cameraChunk.Y + dz);
				int ring = GetRingFromChunkCoordinate(coord);
				if (ring < 1 || ring > maxRing)
				{
					continue;
				}

				if (GetLodForRing(ring) == LOD.Skipdraw)
				{
					continue;
				}

				desired.Add(coord);
			}
		}

		List<Vector2I> toRemove = new List<Vector2I>();
		foreach (KeyValuePair<Vector2I, TerrainChunk> pair in _activeChunks)
		{
			if (!desired.Contains(pair.Key))
			{
				toRemove.Add(pair.Key);
			}
		}

		foreach (Vector2I coord in toRemove)
		{
			TerrainChunk chunk = _activeChunks[coord];
			_activeChunks.Remove(coord);
			chunk.QueueFree();
		}

		foreach (Vector2I coord in desired)
		{
			LOD neededLod = GetLodForRing(GetRingFromChunkCoordinate(coord));

			if (_activeChunks.TryGetValue(coord, out TerrainChunk existing))
			{
				if (existing.BuiltLod != neededLod)
				{
					existing.Generate();
					ApplyTerrainMaterial(existing);
				}
				continue;
			}

			TerrainChunk chunk = new TerrainChunk();
			AddChild(chunk);
			chunk.Setup(coord);
			chunk.Generate();
			ApplyTerrainMaterial(chunk);
			_activeChunks[coord] = chunk;
		}
	}

	private void RebuildAllActiveChunkMeshes()
	{
		foreach (KeyValuePair<Vector2I, TerrainChunk> pair in _activeChunks)
		{
			if (!GodotObject.IsInstanceValid(pair.Value))
			{
				continue;
			}

			pair.Value.Generate();
			ApplyTerrainMaterial(pair.Value);
		}
	}

	private void ClearActiveChunks()
	{
		foreach (KeyValuePair<Vector2I, TerrainChunk> pair in _activeChunks)
		{
			if (GodotObject.IsInstanceValid(pair.Value))
			{
				pair.Value.QueueFree();
			}
		}
		_activeChunks.Clear();

		// Also free any leftover terrain children (e.g. from earlier test spawns).
		Array<Node> children = GetChildren();
		foreach (Node child in children)
		{
			if (child is TerrainChunk)
			{
				child.QueueFree();
			}
		}
	}

	private void ApplyTerrainMaterial(TerrainChunk chunk)
	{
		if (TerrainMaterial is null || chunk.GetChildCount() == 0)
		{
			return;
		}

		if (chunk.GetChild(0) is MeshInstance3D meshInstance)
		{
			meshInstance.MaterialOverride = TerrainMaterial;
		}
	}

	/// <summary>
	/// Returns the <see cref="LOD"/> configured for the given 1-based ring distance.
	/// </summary>
	/// <param name="ring">Ring distance; <c>1</c> is the camera chunk and its neighbors.</param>
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
	/// Returns the ring index that the chunk in <paramref name="chunkCoord"/> is in.
	/// Rings are around the camera.
	/// </summary>
	/// <param name="chunkCoord"></param>
	/// <returns>
	/// Ring distance from the camera's chunk (Chebyshev). The camera's own chunk and its
	/// neighbors are ring 1; a chunk two steps away is ring 2; and so on. Without a
	/// camera, falls back to rings around the generator origin under the +X/-Z layout.
	/// </returns>
	public int GetRingFromChunkCoordinate(Vector2I chunkCoord)
	{
		if (Camera is not null)
		{
			Vector2I cameraChunk = GetChunkCoordinatesFromWorldCoordinates(Camera.GlobalPosition);
			int dx = Mathf.Abs(chunkCoord.X - cameraChunk.X);
			int dz = Mathf.Abs(chunkCoord.Y - cameraChunk.Y);
			int distance = Mathf.Max(dx, dz);
			// Camera chunk (distance 0) shares ring 1 with immediate neighbors.
			return distance == 0 ? 1 : distance;
		}

		// Generator-origin rings (+X/-Z): ring 1 is (-1,0), (-1,1), (0,0), (0,1).
		int ringX = chunkCoord.X >= 0 ? chunkCoord.X + 1 : -chunkCoord.X;
		int ringZ = chunkCoord.Y <= 0 ? 1 - chunkCoord.Y : chunkCoord.Y;
		return Mathf.Max(ringX, ringZ);
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
	/// <c>floor(localX / ChunkSize)</c> and <c>ceil(localZ / ChunkSize)</c>
	/// (chunk origin is the max-Z corner). Local Y (up) is ignored.
	/// </remarks>
	public Vector2I GetChunkCoordinatesFromWorldCoordinates(Vector3 pos)
	{
		if (ChunkSize == 0f)
		{
			return Vector2I.Zero;
		}

		Vector3 local = ToLocal(pos);
		int cx = Mathf.FloorToInt(local.X / ChunkSize);
		int cz = Mathf.CeilToInt(local.Z / ChunkSize);
		return new Vector2I(cx, cz);
	}

}
