using Godot;
using System;

/// <summary>
/// Represents a chunk of terrain, containing it's geometry, collision, and arraymesh plane.
/// Not allowed to have a parent that is not of type TerrainGenerator. 
/// </summary>
/// <remarks>
/// The chunk node's origin is the corner of the chunk. Geometry extends along
/// +X and -Z from that origin (local space occupies <c>[0, size]</c> on X and
/// <c>[-size, 0]</c> on Z).
/// </remarks>
[Tool]
public partial class TerrainChunk : Node3D
{
	private Vector2I _chunkCoordinate;
	private bool _hasCoordinate;

	/// <summary>
	/// The grid coordinate of this chunk. Set via <see cref="Setup"/> when spawned.
	/// </summary>
	public Vector2I ChunkCoordinate => _hasCoordinate ? _chunkCoordinate : GetChunkCoordinateFromPosition();

	/// <summary>
	/// The ring the chunk is in relative to the camera.
	/// </summary>
	public int ChunkRing => GetRing();

	public TerrainGenerator.LOD Lod => GetLod();

	/// <summary>
	/// LOD last successfully built by <see cref="Generate"/>. <see cref="TerrainGenerator.LOD.Skipdraw"/>
	/// if nothing has been built yet or the last build was skipped.
	/// </summary>
	public TerrainGenerator.LOD BuiltLod { get; private set; } = TerrainGenerator.LOD.Skipdraw;

	public int Resolution => GetResolution();
	public float Size => GetSize();

	public override void _Ready()
	{
		// Prevent logic running when it is in tool in it's own scene.
		if (Engine.IsEditorHint() && GetParent() is null)
		{
			return;
		}

		if (GetParent() is not TerrainGenerator)
		{
			throw new InvalidOperationException(
				$"{nameof(TerrainChunk)} requires a parent of type {nameof(TerrainGenerator)}, " +
				$"but parent was {(GetParent() is null ? "null" : GetParent().GetType().Name)}.");
		}
	}

	/// <summary>
	/// Assigns grid identity and places this chunk at the corresponding corner origin.
	/// Must be called after the chunk is parented to a <see cref="TerrainGenerator"/>.
	/// </summary>
	public void Setup(Vector2I chunkCoordinate)
	{
		_chunkCoordinate = chunkCoordinate;
		_hasCoordinate = true;

		TerrainGenerator generator = GetParent() as TerrainGenerator;
		if (generator is null || generator.ChunkSize == 0f)
		{
			return;
		}

		Position = new Vector3(
			chunkCoordinate.X * generator.ChunkSize,
			0f,
			chunkCoordinate.Y * generator.ChunkSize);
	}

	public void Generate()
	{
		TerrainGenerator parent = GetParent() as TerrainGenerator;

		if (parent is null)
		{
			return;
		}

		// Wipe away children first.
		Godot.Collections.Array<Node> children = GetChildren();
		foreach (Node child in children)
		{
			if (child is not null)
			{
				child.Free();
			}
		}

		TerrainGenerator.LOD lod = Lod;
		if (lod == TerrainGenerator.LOD.Skipdraw || Resolution <= 0)
		{
			BuiltLod = TerrainGenerator.LOD.Skipdraw;
			return;
		}

		MeshInstance3D meshInstance = new MeshInstance3D();
		AddChild(meshInstance);
		// Displaced mesh is already corner-origin in [0, Size] on X and [-Size, 0] on Z.
		meshInstance.Mesh = BuildDisplacedMesh();
		meshInstance.Position = Vector3.Zero;
		BuiltLod = lod;
	}

	/// <summary>
	/// Returns an ArrayMesh corresponding to a flat plane with a subdivide
	/// width and depth equal to quadCount and a size equal to size.
	/// </summary>
	/// <remarks>
	/// The plane is centered on its mesh origin. Callers must offset by
	/// <c>(size / 2, 0, -size / 2)</c> so the plane occupies the chunk's +X/-Z extent
	/// relative to the chunk's corner origin.
	/// </remarks>
	/// <returns></returns>
	private ArrayMesh BuildFlatPlane()
	{
		PlaneMesh basePlane = new PlaneMesh();
		basePlane.SubdivideDepth = Resolution;
		basePlane.SubdivideWidth = Resolution;
		basePlane.Size = new Vector2(Size, Size);

		ArrayMesh flatPlaneArrayMesh = new ArrayMesh();
		flatPlaneArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, basePlane.GetMeshArrays());

		return flatPlaneArrayMesh;
	}

	/// <summary>
	/// Generates a displaced plane based on sampling the parent's heightmap, the 
	/// current LOD, the current size, and the current resolution.
	/// </summary>
	/// <remarks>
	/// Vertices occupy local <c>[0, Size]</c> on X and <c>[-Size, 0]</c> on Z (corner origin).
	/// Normals are set to <see cref="Vector3.Up"/> to avoid lighting seams at chunk borders.
	/// </remarks>
	private ArrayMesh BuildDisplacedMesh()
	{
		TerrainGenerator generator = GetParent() as TerrainGenerator;
		int resolution = Resolution;
		float size = Size;

		if (generator is null || resolution <= 0 || size <= 0f)
		{
			return new ArrayMesh();
		}

		int vertsPerSide = resolution + 1;
		int vertCount = vertsPerSide * vertsPerSide;
		Vector3[] vertices = new Vector3[vertCount];
		Vector3[] normals = new Vector3[vertCount];
		Vector2[] uvs = new Vector2[vertCount];

		float invRes = 1f / resolution;

		for (int zi = 0; zi < vertsPerSide; zi++)
		{
			for (int xi = 0; xi < vertsPerSide; xi++)
			{
				float u = xi * invRes;
				float v = zi * invRes;
				float x = u * size;
				float z = -v * size;

				Vector3 world = ToGlobal(new Vector3(x, 0f, z));
				// One heightmap tile per chunk: UV advances by 1 across each ChunkSize.
				float height = generator.SampleHeightmap(new Vector2(
						world.X / generator.ChunkSize,
						world.Z / generator.ChunkSize))
					* generator.HeightScale;

				int i = zi * vertsPerSide + xi;
				vertices[i] = new Vector3(x, height, z);
				uvs[i] = new Vector2(u, v);
				normals[i] = Vector3.Up;
			}
		}

		int[] indices = new int[resolution * resolution * 6];
		int idx = 0;
		for (int zi = 0; zi < resolution; zi++)
		{
			for (int xi = 0; xi < resolution; xi++)
			{
				int i0 = zi * vertsPerSide + xi;
				int i1 = i0 + 1;
				int i2 = i0 + vertsPerSide;
				int i3 = i2 + 1;

				indices[idx++] = i0;
				indices[idx++] = i2;
				indices[idx++] = i1;
				indices[idx++] = i1;
				indices[idx++] = i2;
				indices[idx++] = i3;
			}
		}

		Godot.Collections.Array arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	/// <summary>
	/// Returns the chunk coordinate derived from <see cref="Node3D.Position"/>.
	/// </summary>
	private Vector2I GetChunkCoordinateFromPosition()
	{
		TerrainGenerator generator = GetParent() as TerrainGenerator;
		if (generator is null || generator.ChunkSize == 0f)
		{
			return Vector2I.Zero;
		}

		float size = generator.ChunkSize;
		int cx = Mathf.FloorToInt(Position.X / size);
		int cz = Mathf.FloorToInt(Position.Z / size);
		return new Vector2I(cx, cz);
	}

	/// <summary>
	/// Returns the ring the chunk is in based on its chunk coordinate.
	/// </summary>
	private int GetRing()
	{
		TerrainGenerator generator = GetParent() as TerrainGenerator;
		if (generator is null)
		{
			return 0;
		}

		return generator.GetRingFromChunkCoordinate(ChunkCoordinate);
	}

	/// <summary>
	/// Returns the LOD of the current chunk based on the ring it is part of.
	/// </summary>
	private TerrainGenerator.LOD GetLod()
	{
		TerrainGenerator generator = GetParent() as TerrainGenerator;
		if (generator is null)
		{
			return TerrainGenerator.LOD.Skipdraw;
		}

		return generator.GetLodForRing(ChunkRing);
	}

	/// <summary>
	/// Returns the resolution of the current chunk by calling parent's GetResolutionByLod
	/// with current LOD.
	/// </summary>
	private int GetResolution()
	{
		TerrainGenerator generator = GetParent() as TerrainGenerator;
		if (generator is null)
		{
			return 0;
		}

		return generator.GetResolutionByLod(Lod);
	}

	/// <summary>
	/// Returns the size of the current chunk based on the parent's generated size.
	/// </summary>
	private float GetSize()
	{
		TerrainGenerator generator = GetParent() as TerrainGenerator;
		if (generator is null)
		{
			return 0f;
		}

		return generator.ChunkSize;
	}
}
