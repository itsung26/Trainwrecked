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
[Icon("res://addons/at-icons/mesh/subdivision.svg")]
public partial class TerrainChunk : Node3D
{
	private Vector2I _chunkCoordinate;
	private bool _hasCoordinate;

	/// <summary>
	/// Editor gizmo scene instantiated at the chunk center while running in the editor.
	/// </summary>
	public PackedScene IconScene { get; set; }

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
		if (Engine.IsEditorHint())
		{
            if (GetParent() is null)
            {
                return;
            }
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

		// Add an icon in the editor to show chunk centers.
		if (Engine.IsEditorHint() && IconScene is not null)
		{
			Node icon = IconScene.Instantiate();
			AddChild(icon);
			if (icon is Node3D icon3D)
			{
				Vector2 center = GetChunkCenter();
				icon3D.GlobalPosition = new Vector3(center.X, parent.HeightFunction.HeightFunctionScale, center.Y);
			}
		}

		MeshInstance3D meshInstance = new MeshInstance3D();
		AddChild(meshInstance);
		// Displaced mesh is already corner-origin in [0, Size] on X and [-Size, 0] on Z.
		meshInstance.Mesh = BuildDisplacedMesh();
		meshInstance.Position = Vector3.Zero;

		HeightMapShape3D heightMapShape = BuildDisplacedMeshCollisionShape();
		if (heightMapShape is not null)
		{
			StaticBody3D staticBody = new StaticBody3D();
			AddChild(staticBody);

			CollisionShape3D collisionShape = new CollisionShape3D();
			staticBody.AddChild(collisionShape);
			collisionShape.Shape = heightMapShape;
			// HeightMapShape3D is centered with 1-unit spacing; align to chunk +X/-Z extent.
			int collisionResolution = parent.FullLodCollsionResolution;
			collisionShape.Position = new Vector3(Size / 2f, 0f, -Size / 2f);
			float cellSize = Size / collisionResolution;
			collisionShape.Scale = new Vector3(cellSize, 1f, cellSize);
		}

		BuiltLod = lod;
	}

	/// <summary>
	/// Generates a displaced plane based on sampling the parent's height function, the 
	/// current LOD, the current size, and the current resolution.
	/// </summary>
	/// <remarks>
	/// Vertices occupy local <c>[0, Size]</c> on X and <c>[-Size, 0]</c> on Z (corner origin).
	/// Normals are derived from the finished height grid via central differences.
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
				float height = generator.SampleHeight(new Vector2(world.X, world.Z));

				int i = zi * vertsPerSide + xi;
				vertices[i] = new Vector3(x, height, z);
				uvs[i] = new Vector2(u, v);
			}
		}

		ComputeGridNormals(vertices, normals, vertsPerSide);

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
	/// Fills <paramref name="normals"/> from the regular height grid in <paramref name="vertices"/>.
	/// Uses central differences where possible and one-sided differences on borders.
	/// </summary>
	private static void ComputeGridNormals(Vector3[] vertices, Vector3[] normals, int vertsPerSide)
	{
		for (int zi = 0; zi < vertsPerSide; zi++)
		{
			for (int xi = 0; xi < vertsPerSide; xi++)
			{
				int row = zi * vertsPerSide;
				Vector3 left = vertices[row + Math.Max(xi - 1, 0)];
				Vector3 right = vertices[row + Math.Min(xi + 1, vertsPerSide - 1)];
				// Grid +zi goes toward world -Z; world +Z neighbor is smaller zi.
				Vector3 towardNegZ = vertices[Math.Min(zi + 1, vertsPerSide - 1) * vertsPerSide + xi];
				Vector3 towardPosZ = vertices[Math.Max(zi - 1, 0) * vertsPerSide + xi];

				Vector3 tangentX = right - left;
				Vector3 tangentZ = towardPosZ - towardNegZ;
				Vector3 normal = tangentZ.Cross(tangentX);
				normals[row + xi] = normal.LengthSquared() > 1e-12f ? normal.Normalized() : Vector3.Up;
			}
		}
	}

	/// <summary>
	/// Generates a <see cref="HeightMapShape3D"/> for collision using the parent's height function.
	/// Only generates for the <see cref="TerrainGenerator.LOD.Full"/> LOD; lower LODs return
	/// <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// Uses <see cref="TerrainGenerator.FullLodCollsionResolution"/> (not visual mesh resolution).
	/// The shape is centered with 1-unit vertex spacing; place the <see cref="CollisionShape3D"/> at
	/// <c>(Size / 2, 0, -Size / 2)</c> and scale XZ by
	/// <c>Size / FullLodCollsionResolution</c>.
	/// </remarks>
	/// <returns></returns>
	private HeightMapShape3D BuildDisplacedMeshCollisionShape()
	{
		TerrainGenerator generator = GetParent() as TerrainGenerator;
		if (generator is null || Lod != TerrainGenerator.LOD.Full)
		{
			return null;
		}

		int resolution = generator.FullLodCollsionResolution;
		float size = Size;
		if (resolution <= 0 || size <= 0f)
		{
			return null;
		}

		int vertsPerSide = resolution + 1;
		float invRes = 1f / resolution;
		float[] heights = new float[vertsPerSide * vertsPerSide];

		// HeightMapShape3D depth increases in +Z. With a CollisionShape centered at
		// (size/2, 0, -size/2), zi=0 maps to world z=-size and zi=max to z=0.
		for (int zi = 0; zi < vertsPerSide; zi++)
		{
			for (int xi = 0; xi < vertsPerSide; xi++)
			{
				float u = xi * invRes;
				float v = zi * invRes;
				float x = u * size;
				float z = -size + v * size;

				Vector3 world = ToGlobal(new Vector3(x, 0f, z));
				float height = generator.SampleHeight(new Vector2(world.X, world.Z));

				heights[zi * vertsPerSide + xi] = height;
			}
		}

		HeightMapShape3D shape = new HeightMapShape3D();
		shape.MapWidth = vertsPerSide;
		shape.MapDepth = vertsPerSide;
		shape.MapData = heights;
		return shape;
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

	/// <summary>
	/// Returns the horizontal center of the chunk in world XZ.
	/// Local center is <c>(Size / 2, -Size / 2)</c> from the corner origin.
	/// </summary>
	public Vector2 GetChunkCenter()
	{
		Vector3 world = ToGlobal(new Vector3(Size / 2f, 0f, -Size / 2f));
		return new Vector2(world.X, world.Z);
	}
}
