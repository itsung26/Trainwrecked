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
[GlobalClass, Icon("res://addons/at-icons/mesh/mountains.svg")]
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
	/// Editor gizmo scene instantiated under this node while running in the editor.
	/// </summary>
	[Export] public PackedScene IconScene { get; set; }

	/// <summary>
	/// Editor gizmo scene assigned to spawned <see cref="TerrainChunk"/> instances.
	/// </summary>
	[Export] public PackedScene ChunkIconScene { get; set; }

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
	/// <summary>
	/// Height provider sampled when building chunk meshes and collision.
	/// </summary>
	[Export] public HeightFunctionSampler HeightFunction { get; set; }
	[Export] public BaseMaterial3D TerrainMaterial { get; set; }
	/// <summary>
	/// Vertex resolution (quads per axis) used when building Full-LOD
	/// <see cref="HeightMapShape3D"/> collision. Independent of <see cref="FullLodResolution"/>
	/// so collision can stay cheaper than the visual mesh.
	/// </summary>
	[Export] public int FullLodCollsionResolution { get; set; }
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

	private readonly System.Collections.Generic.Dictionary<Vector2I, TerrainChunk> _activeChunks = new System.Collections.Generic.Dictionary<Vector2I, TerrainChunk>();
	private Vector2I _lastCameraChunk;
	private bool _hasLastCameraChunk;

	[ExportToolButton("Generate Terrain", Icon = "MeshInstance3D")]
	public Callable GenerateTerrainButton => Callable.From(GenerateTerrain);


	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			if (IconScene is not null)
			{
				AddChild(IconScene.Instantiate());
			}
			return;
		}

		CallDeferred(MethodName.UpdateChunks, true);
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
	/// Returns the height from <see cref="HeightFunction"/> at <paramref name="uv"/>.
	/// </summary>
	/// <param name="uv">World XZ sample position forwarded to <see cref="HeightFunctionSampler.Sample(Vector2)"/>.</param>
	/// <returns>
	/// Sampled height, or <c>0</c> when <see cref="HeightFunction"/> is unset.
	/// </returns>
	public float SampleHeight(Vector2 uv)
	{
		if (HeightFunction is null)
		{
			return 0f;
		}

		return HeightFunction.Sample(uv);
	}

	/// <summary>
	/// Clears active chunks and force-streams around the camera.
	/// </summary>
	public void GenerateTerrain()
	{
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

		Vector2I cameraChunk = GetChunkCoordinatesFromWorldCoordinates(Camera.GlobalPosition);
		if (!force && _hasLastCameraChunk && cameraChunk == _lastCameraChunk)
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
			chunk.IconScene = ChunkIconScene;
			chunk.Setup(coord);
			chunk.Generate();
			ApplyTerrainMaterial(chunk);
			_activeChunks[coord] = chunk;
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
		if (TerrainMaterial is null)
		{
			return;
		}

		foreach (Node child in chunk.GetChildren())
		{
			if (child is MeshInstance3D meshInstance)
			{
				meshInstance.MaterialOverride = TerrainMaterial;
				return;
			}
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
