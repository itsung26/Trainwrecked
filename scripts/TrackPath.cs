using Godot;
using Godot.Collections;
using System;

[Tool]
[GlobalClass, Icon("res://assets/2d assets/traintracks_classicon.png")]
public partial class TrackPath : TrainwreckedPath
{
	private const String GeneratedTrackMeta = "generated_track";
	// // The track keeping references of things on it?
	// public Array<RollingStock> Rollers { get; private set; }

	[Export] public PackedScene TrackScene = null;
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

	[ExportToolButton("Smoothen Track Curve", Icon = "Curve3D")]
	public Callable EnforceCurveRestrictionsButton => Callable.From(EnforceCurveRestrictions);

	public override void _Ready()
	{
		CurveChanged += _on_curve_changed;
		RebuildTracks();
	}

	/// <summary>
	/// Instantiates <see cref="TrackScene"/> along the curve at <see cref="TrackSpacing"/>
	/// intervals so the full baked length is covered.
	/// </summary>
	public void RebuildTracks()
	{
		ClearGeneratedTracks();

		if (TrackScene == null || Curve == null || TrackSpacing <= 0f)
		{
			return;
		}

		float pathLength = Curve.GetBakedLength();
		if (pathLength <= 0f)
		{
			return;
		}

		// Include both endpoints: 0, spacing, ..., floor(length/spacing)*spacing, and length if needed.
		int trackCount = Mathf.FloorToInt(pathLength / TrackSpacing) + 1;
		for (int i = 0; i < trackCount; i++)
		{
			float offset = Mathf.Min(i * TrackSpacing, pathLength);

			Node3D track = TrackScene.Instantiate<Node3D>();
			track.SetMeta(GeneratedTrackMeta, true);
			AddChild(track);
			track.Transform = Curve.SampleBakedWithRotation(offset, cubic: false, applyTilt: true);
			track.Scale = new Vector3(TrackMeshScale, TrackMeshScale, TrackMeshScale);
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

	/// <summary>
	/// Sets each control point's in/out handles from neighbor positions (Catmull-Rom style)
	/// so the curve stays G1-smooth through the waypoints.
	/// </summary>
	/// <returns>The modified <see cref="Path3D.Curve"/>, or <see langword="null"/> if unavailable.</returns>
	private void EnforceCurveTangents()
	{
		Curve3D curve = Curve;
		if (curve is null)
		{
			return;
		}

		int pointCount = curve.PointCount;
		for (int i = 0; i < pointCount; i++)
		{
			Vector3 prev = curve.GetPointPosition(Mathf.Max(i - 1, 0));
			Vector3 next = curve.GetPointPosition(Mathf.Min(i + 1, pointCount - 1));

			// Bezier handle = Catmull-Rom tangent / 3 = (next - prev) / 6.
			Vector3 handleOut = (next - prev) / 6.0f;
			curve.SetPointOut(i, handleOut);
			curve.SetPointIn(i, -handleOut);
		}
	}

	private void EnforceCurveElevation()
	{
		Curve3D curve = Curve;
		if (curve is null)
		{
			return;
		}

		int pointCount = curve.PointCount;
		for (int i = 0; i < pointCount; i++)
		{
			Vector3 pointDisplacedPosition = new Vector3(curve.GetPointPosition(i).X, 0.0f, curve.GetPointPosition(i).Z);
			curve.SetPointPosition(i, pointDisplacedPosition);
		}
	}

	/// <summary>
	/// Applies track curve restrictions (smooth Catmull-Rom handles for now), then rebuilds meshes.
	/// </summary>
	public void EnforceCurveRestrictions()
	{
		// Momentarily disconnect the signal to prevent the signal emitting during every
		// change in the validation cycle.
		CurveChanged -= _on_curve_changed;
		try
		{
			// Order does matter here- tangents will not calculate correctly if
			// elevation of the track is not uniform.
			EnforceCurveElevation();
			EnforceCurveTangents();
		}
		finally
		{
			CurveChanged += _on_curve_changed;
		}

		RebuildTracks();
	}

	public void _on_curve_changed()
	{
		RebuildTracks();
	}


}
