using Godot;
using System;

[Tool]
[GlobalClass]
public partial class TrackPath : Path3D
{
	private const String GeneratedTrackMeta = "generated_track";

	[Export] public PackedScene TrackScene = null;

	private int _trackCount = 1;
	[Export]
	public int TrackCount
	{
		get { return _trackCount; }
		set
		{
			_trackCount = value;
			if (IsNodeReady())
			{
				RebuildTracks();
			}
		}
	}

	private float _trackSpacing = 1.0f;
	[Export]
	public float TrackSpacing
	{
		get { return _trackSpacing; }
		set
		{
			_trackSpacing = value;
			if (IsNodeReady())
			{
				RebuildTracks();
			}
		}
	}

	private float _trackMeshScale = 1.0f;
	[Export]
	public float TrackMeshScale
	{
		get { return _trackMeshScale; }
		set
		{
			_trackMeshScale = value;
			if (IsNodeReady())
			{
				RebuildTracks();
			}
		}
	}

	public override void _Ready()
	{
		CurveChanged += _on_curve_changed;
		RebuildTracks();
	}

	// Creates instances of TrackScene at the specified track count and spacing along the path.
	public void RebuildTracks()
	{
		ClearGeneratedTracks();

		if (TrackScene == null || Curve == null || TrackCount <= 0)
		{
			return;
		}

		float PathLength = Curve.GetBakedLength();
		for (int i = 0; i < TrackCount; i++)
		{
			float Offset = i * TrackSpacing;
			if (PathLength > 0.0f)
			{
				Offset = Mathf.Min(Offset, PathLength);
			}

			Node3D Track = TrackScene.Instantiate<Node3D>();
			Track.SetMeta(GeneratedTrackMeta, true);
			AddChild(Track);
			Track.Transform = Curve.SampleBakedWithRotation(Offset);
			Track.Scale = new Vector3(TrackMeshScale, TrackMeshScale, TrackMeshScale);
		}
	}

	private void ClearGeneratedTracks()
	{
		Godot.Collections.Array<Node> Children = GetChildren();
		for (int i = Children.Count - 1; i >= 0; i--)
		{
			Node Child = Children[i];
			if (Child.HasMeta(GeneratedTrackMeta))
			{
				Child.Free();
			}
		}
	}

	public Vector3 GetWorldPositionAtProgress(float progressRatio)
	{
		Vector3 ret = Vector3.Zero;
		PathFollow3D Sampler = new PathFollow3D();
		AddChild(Sampler);
		Sampler.ProgressRatio = progressRatio;
		ret = Sampler.GlobalPosition;
		Sampler.QueueFree();
		return ret;
	}

	public void _on_curve_changed()
	{
		RebuildTracks();
	}
}
