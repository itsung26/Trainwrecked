using Godot;
using System;

/// <summary>
/// Represents a chunk of terrain, containing it's geometry, collision, and arraymesh plane.
/// Not allowed to have a parent that is not of type TerrainGenerator.
/// </summary>
[Tool]
public partial class TerrainChunk : Node3D
{
	public override void _Ready()
	{
		// Prevent logic running when it is in tool in it's own scene.
		if (Engine.IsEditorHint() && Owner == null)
		{
			return;
		}
	}

	public void GenerateChunk(TerrainGenerator.LOD lod, Texture2D heightmap)
	{
        // Wipe away children first.
        foreach (Node child in GetChildren())
        {
            if (child is not null)
            {
                child.Free();
            }
        }

		switch (lod)
		{
			case TerrainGenerator.LOD.Full:
                MeshInstance3D meshInstance = new MeshInstance3D();
                AddChild(meshInstance);
                meshInstance.Mesh = GenerateFlatPlane(32, 32);
                meshInstance.Position = new Vector3(meshInstance.Position.X + 32 / 2, meshInstance.Position.Y, meshInstance.Position.Z + 32 / 2);
				break;

            case TerrainGenerator.LOD.Half:
                break;

            case TerrainGenerator.LOD.Quarter:
                break;
            
            case TerrainGenerator.LOD.Skipdraw:
                break;

			default:
				break;
		}
	}

    /// <summary>
    /// Returns an ArrayMesh corresponding to a flat plane with a subdivide
    /// width and depth equal to resolution and a size equal to size.
    /// </summary>
    /// <returns></returns>
    private ArrayMesh GenerateFlatPlane(int resolution, float size)
    {
        PlaneMesh basePlane = new PlaneMesh();
        basePlane.SubdivideDepth = resolution;
        basePlane.SubdivideWidth = resolution;
        basePlane.Size = new Vector2(size, size);

        ArrayMesh flatPlaneArrayMesh = new ArrayMesh();
        flatPlaneArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, basePlane.GetMeshArrays());

        return flatPlaneArrayMesh;
    }
}
