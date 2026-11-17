using Godot;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class SelectionWheel : Control
{
	#region Backing Fields
	private float _segmentSpacing = 1.0f;
	private float _wheelRadius = 25.0f;
	private float _ringRadius = 25.0f;
	private float _ringThickness = 50.0f;
	private int _wheelResolution = 32;
	private Color _ringColor = Colors.White;
	private Color _wheelColor = Colors.White;
	private Dictionary<Resource, float> _wheelSlots = new();
	#endregion

	#region Public Properties
	[Export] public float SegmentSpacing
	{
		get => _segmentSpacing;
		set
		{
			_segmentSpacing = Mathf.Max(value, 0.0f);
			Update();
		}
	}

	[Export] public float WheelRadius
	{
		get => _wheelRadius;
		set
		{
			_wheelRadius = value;
			Update();
		}
	}

	[Export] public float RingRadius
	{
		get => _ringRadius;
		set
		{
			_ringRadius = Mathf.Max(value, 0.0f);
			Update();
		}
	}

	[Export] public float RingThickness
	{
		get => _ringThickness;
		set
		{
			_ringThickness = Mathf.Max(value, 0.0f);
			Update();
		}
	}

	[Export] public int WheelResolution
	{
		get => _wheelResolution;
		set
		{
			_wheelResolution = Mathf.Clamp(value, 3, 256);
			Update();
		}
	}

	[Export] public Color RingColor
	{
		get => _ringColor;
		set
		{
			_ringColor = value;
			Update();
		}
	}

	[Export] public Color WheelColor
	{
		get => _wheelColor;
		set
		{
			_wheelColor = value;
			Update();
		}
	}

	[Export] public Dictionary<Resource, float> WheelSlots
	{
		get => _wheelSlots;
		set
		{
			_wheelSlots = value;
			Debug.Log(value.Keys.Count);
			Update();
		}
	}
	#endregion

	[ExportGroup("Node Refs")]
	[Export] public Polygon2D WheelPolygon { get; set; }
	[Export] public Polygon2D RingPolygon { get; set; }

    public override string[] _GetConfigurationWarnings()
    {
		string[] baseWarnings = base._GetConfigurationWarnings() ?? System.Array.Empty<string>();
        Array<string> warnings = new Array<string>(baseWarnings);

		if (string.IsNullOrEmpty(SceneFilePath))
		{
			warnings.Add("Should not be instanced directly.");
		}

		return warnings.ToArray<string>();
    }

    public override void _Ready()
    {
        LockWheelToCenter();
		Update();
    }

    public override void _Process(double delta)
    {
        LockWheelToCenter();
    }

	/// Keeps the wheel in the center of this control's rectangle.
	private void LockWheelToCenter()
	{
		ArgumentNullException.ThrowIfNull(WheelPolygon);
		ArgumentNullException.ThrowIfNull(RingPolygon);

		Vector2 center = GetRect().GetCenter();

		WheelPolygon.GlobalPosition = center;
		RingPolygon.GlobalPosition = center;
	}

	/// Redraws the entire wheel based on exported parameters.
	private void Update()
	{
		if (WheelPolygon is null || RingPolygon is null)
		{
			return;
		}

		DrawCircle();
		DrawRing();

		// Update colors.
		WheelPolygon.Color = WheelColor;
		RingPolygon.Color = RingColor;
	}

	private void DrawCircle()
	{
		int resolution = Mathf.Clamp(WheelResolution, 3, 256);

		if (Mathf.IsEqualApprox(WheelRadius, 0.0f))
		{
			WheelPolygon.Polygon = System.Array.Empty<Vector2>();
			return;
		}

		Vector2[] circle = new Vector2[resolution];
		float angle = 0.0f;

		for (int i = 0; i < circle.Length; i++)
		{
			Vector2 point = ToRectangular(new Vector2(WheelRadius, angle));
			circle[i] = point;
			angle += (2.0f * Mathf.Pi) / resolution;
		}

		WheelPolygon.Polygon = circle;
	}

	/// Builds a segmented ring as explicit triangles between the outer and inner arcs.
	private void DrawRing()
	{
		if (RingPolygon is null)
		{
			throw new InvalidOperationException();
		}

		int segments = GetSegmentCount();
		int resolution = Mathf.Clamp(WheelResolution, 3, 256);
		float radius = Mathf.Max(RingRadius, 0.0f);
		float thickness = Mathf.Max(RingThickness, 0.0f);

		if (Mathf.IsZeroApprox(thickness))
		{
			RingPolygon.Polygon = System.Array.Empty<Vector2>();
			RingPolygon.Polygons = new Godot.Collections.Array();
			return;
		}

		float innerRadius = radius;
		float outerRadius = radius + thickness;
		float segmentSpan = Mathf.Tau / segments;
		// SegmentSpacing is degrees of gutter between slices (none when there's only one segment).
		float gap = segments == 1 ? 0.0f : Mathf.DegToRad(SegmentSpacing);
		gap = Mathf.Min(gap, segmentSpan * 0.95f);
		float arcSpan = segmentSpan - gap;
		int stepsPerSegment = Mathf.Max(2, Mathf.CeilToInt((float)resolution / segments));
		int pointsPerSegment = stepsPerSegment + 1;

		Vector2[] vertices = new Vector2[segments * pointsPerSegment * 2];
		Godot.Collections.Array triangles = new();

		for (int segment = 0; segment < segments; segment++)
		{
			float startAngle = segment * segmentSpan + gap * 0.5f;
			int outerBase = segment * pointsPerSegment * 2;
			int innerBase = outerBase + pointsPerSegment;

			for (int i = 0; i < pointsPerSegment; i++)
			{
				float angle = startAngle + arcSpan * i / stepsPerSegment;
				Vector2 direction = Vector2.FromAngle(angle);

				vertices[outerBase + i] = direction * outerRadius;
				vertices[innerBase + i] = direction * innerRadius;
			}

			for (int i = 0; i < stepsPerSegment; i++)
			{
				int outer0 = outerBase + i;
				int outer1 = outerBase + i + 1;
				int inner0 = innerBase + i;
				int inner1 = innerBase + i + 1;

				triangles.Add(new int[] { outer0, outer1, inner0 });
				triangles.Add(new int[] { outer1, inner1, inner0 });
			}
		}

		RingPolygon.Polygon = vertices;
		RingPolygon.Polygons = triangles;
	}

	/// Converts rectangular (x, y) to polar (radius, angle in radians).
	private static Vector2 ToPolar(Vector2 point)
	{
		return new Vector2(point.Length(), point.Angle());
	}

	/// Converts polar (radius, angle in radians) to rectangular (x, y).
	private static Vector2 ToRectangular(Vector2 polarCoord)
	{
		return Vector2.FromAngle(polarCoord.Y) * polarCoord.X;
	}

	/// Derive segment count from the amount of elements in WheelSlots.
	private int GetSegmentCount()
	{
		if (WheelSlots is null)
		{
			throw new InvalidOperationException();
		}

		if (WheelSlots.Count == 0)
		{
			return 1;
		}

		return WheelSlots.Count;
	}
}
