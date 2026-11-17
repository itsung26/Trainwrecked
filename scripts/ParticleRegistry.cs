using System;
using System.Diagnostics.CodeAnalysis;
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
        PopulateCatalog();
    }

    private void PopulateCatalog()
    {
        Array<string> names = GetParticlePackedSceneRootNodeNames();
        Array<PackedScene> scenes = GetGpuParticlePackedScenes();

        if (names.Count != scenes.Count)
        {
            throw new IndexOutOfRangeException(
                $"Particle catalog name/scene count mismatch: {names.Count} names, {scenes.Count} scenes.");
        }

        for (int i = 0; i < names.Count; i++)
        {
            _catalog.Add(names[i], scenes[i]);
        }
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

    /// Returns packed scenes under ParticleAssetsPath whose root node is a GpuParticles3D.
    private Array<PackedScene> GetGpuParticlePackedScenes()
    {
        Array<string> foundFiles = GetSceneFilePaths();
        Array<PackedScene> scenes = new Array<PackedScene>();

        foreach (string fileName in foundFiles)
        {
            PackedScene currentScene = GD.Load<PackedScene>(fileName);
            if (currentScene is null)
            {
                continue;
            }

            Node currentRoot = currentScene.Instantiate();
            if (currentRoot is GpuParticles3D)
            {
                scenes.Add(currentScene);
            }
            currentRoot.Free();
        }

        return scenes;
    }

    private Array<string> GetParticlePackedSceneRootNodeNames()
    {
        Array<string> names = new Array<string>();
        Array<PackedScene> particlePackedScenes = GetGpuParticlePackedScenes();

        foreach (var particlePackedScene in particlePackedScenes)
        {
            GpuParticles3D current = particlePackedScene.Instantiate<GpuParticles3D>();
            names.Add(current.Name);
            current.QueueFree();
        }

        return names;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnParticleLocal(string name, Vector3 globalPos)
    {
        if (!Instance._catalog.ContainsKey(name))
        {
            Debug.LogWarn($"Failed to find particle asset named {name}.");
            return;
        }

        PackedScene particleScene = Instance._catalog[name];
        GpuParticles3D particle = particleScene.Instantiate<GpuParticles3D>();

        // Non one-shot particles are not currently supported.
        if (!particle.OneShot)
        {
            Debug.LogWarn($"Particle '{name}' is not OneShot; forcing OneShot.");
            particle.OneShot = true;
        }

        Instance.GetTree().CurrentScene.AddChild(particle);
        particle.GlobalPosition = globalPos;
        particle.Finished += () => {
            particle.QueueFree();
        };
        particle.Restart();
    }

    public static void SpawnParticle(string name, Vector3 globalPos)
    {
        Instance.Rpc(MethodName.SpawnParticleLocal, name, globalPos);
    }
}
