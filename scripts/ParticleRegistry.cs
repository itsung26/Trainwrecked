using Godot;
using Godot.Collections;

public partial class ParticleRegistry : Node
{
    private const string ParticleAssetsPath = "res://assets/particles/";

    public static ParticleRegistry Instance { get; private set; }

    /// Maps particle names to PackedScene files with a GPUparticle3D root node.
    private readonly System.Collections.Generic.Dictionary<string, PackedScene> _catalog = new System.Collections.Generic.Dictionary<string, PackedScene>();
    private readonly Array<string> _foundFiles = new Array<string>();

    public override void _Ready()
    {
        Instance = this;
        ReadFiles();
        Debug.Log(GetParticlePackedSceneRootNodeNames());
    }

    /// Called once on ready to read the particle asset directory.
    private void ReadFiles()
    {
        string[] foundFiles = DirAccess.GetFilesAt(ParticleAssetsPath);
        foreach (string file in foundFiles)
        {
            _foundFiles.Add(file);
        }
    }

    /// Returns an array of absolute paths of all of the files in ParticleAssetsPath.
    private Array<string> GetSceneFilePaths()
    {
        Array<string> paths = new Array<string>();

        foreach (string fileName in _foundFiles)
        {
            if (!fileName.EndsWith(".tscn"))
            {
                continue;
            }

            paths.Add(ParticleAssetsPath.PathJoin(fileName));
        }

        return paths;
    }

    /// Returns an array of PackedScenes with a root node type of T.
    private Array<PackedScene> GetPackedScenes<T>() where T : Node
    {
        Array<string> foundFiles = GetSceneFilePaths();
        Array<PackedScene> scenes = new Array<PackedScene>();

        foreach (string fileName in foundFiles)
        {
            PackedScene currentScene = GD.Load<PackedScene>(fileName);
            Node currentRoot = currentScene.Instantiate();
            if (currentRoot is T)
            {
                scenes.Add(currentScene);
            }
            currentRoot.QueueFree();
        }

        return scenes;
    }

    private Array<string> GetParticlePackedSceneRootNodeNames()
    {
        Array<string> names = new Array<string>();
        Array<PackedScene> particlePackedScenes = GetPackedScenes<GpuParticles3D>();

        foreach (var particlePackedScene in particlePackedScenes)
        {
            GpuParticles3D current = particlePackedScene.Instantiate<GpuParticles3D>();
            names.Add(current.Name);
        }

        return names;
    }

    // public void SpawnParticle(string name, Vector3 globalPos)
    // {

    // }

    // public void SpawnParticle(string name, Transform3D globalTransform)
    // {
        
    // }
}
