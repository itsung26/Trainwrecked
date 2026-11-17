using System;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;

/// <summary>
/// Autoload that catalogs <see cref="GpuParticles3D"/> scenes under <c>res://assets/particles/</c>
/// and spawns them by root-node name over the network.
/// </summary>
/// <remarks>
/// Call <see cref="SpawnParticle"/> from gameplay code. Catalog keys are the root
/// <see cref="Node.Name"/> of each packed scene (for example <c>SparksSharp</c>), not the file name.
/// Only one-shot particles are supported; non-one-shot assets are forced to one-shot at spawn time.
/// </remarks>
public partial class ParticleRegistry : Node
{
    /// <summary>Directory scanned for particle <c>.tscn</c> assets.</summary>
    private const string ParticleAssetsPath = "res://assets/particles/";

    /// <summary>Singleton set in <see cref="_Ready"/>.</summary>
    public static ParticleRegistry Instance { get; private set; }

    /// <summary>
    /// Maps particle root-node names to packed scenes whose root is a <see cref="GpuParticles3D"/>.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, PackedScene> _catalog = new System.Collections.Generic.Dictionary<string, PackedScene>();

    /// <summary>File names discovered under <see cref="ParticleAssetsPath"/> during <see cref="ReadFiles"/>.</summary>
    private readonly Array<string> _foundFiles = new Array<string>();

    /// <summary>
    /// Assigns <see cref="Instance"/>, reads the particle asset directory, and builds <see cref="_catalog"/>.
    /// </summary>
    public override void _Ready()
    {
        Instance = this;
        ReadFiles();
        PopulateCatalog();
    }

    /// <summary>
    /// Fills <see cref="_catalog"/> by pairing root-node names with matching <see cref="GpuParticles3D"/> scenes.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the name list and scene list lengths differ.
    /// </exception>
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

    /// <summary>
    /// Reads every file name under <see cref="ParticleAssetsPath"/> into <see cref="_foundFiles"/>.
    /// </summary>
    private void ReadFiles()
    {
        string[] foundFiles = DirAccess.GetFilesAt(ParticleAssetsPath);
        foreach (string file in foundFiles)
        {
            _foundFiles.Add(file);
        }
    }

    /// <summary>
    /// Returns absolute <c>res://</c> paths for each <c>.tscn</c> file under <see cref="ParticleAssetsPath"/>.
    /// </summary>
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

    /// <summary>
    /// Loads packed scenes under <see cref="ParticleAssetsPath"/> whose root node is a <see cref="GpuParticles3D"/>.
    /// </summary>
    /// <returns>Scenes that passed the root-type check; unloadable or non-particle roots are skipped.</returns>
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

    /// <summary>
    /// Returns the root <see cref="Node.Name"/> of each catalogable <see cref="GpuParticles3D"/> packed scene.
    /// </summary>
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

    /// <summary>
    /// Instantiates the named particle at <paramref name="globalPos"/> on every peer that receives the RPC.
    /// </summary>
    /// <param name="name">Catalog key: root <see cref="Node.Name"/> of a registered particle scene.</param>
    /// <param name="globalPos">World-space position for the spawned particle.</param>
    /// <remarks>
    /// Missing names log a warning and return. If the asset is not one-shot, <see cref="GpuParticles3D.OneShot"/>
    /// is forced to <see langword="true"/>. The instance is freed when <see cref="GpuParticles3D.Finished"/> fires.
    /// </remarks>
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

    /// <summary>
    /// Requests a networked spawn of the named particle at <paramref name="globalPos"/>.
    /// </summary>
    /// <param name="name">Catalog key: root <see cref="Node.Name"/> of a registered particle scene.</param>
    /// <param name="globalPos">World-space position forwarded to <see cref="SpawnParticleLocal"/>.</param>
    public static void SpawnParticle(string name, Vector3 globalPos)
    {
        Instance.Rpc(MethodName.SpawnParticleLocal, name, globalPos);
    }
}
