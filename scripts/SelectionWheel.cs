using Godot;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class SelectionWheel : Control
{
	private float _segmentSpacing = 1.0f;
	private float _ringRadius = 25.0f;
	private float _ringThickness = 50.0f;
	private int _wheelResolution = 32;

	[Export] public float WheelRotation { get; set; } = 0.0f;
	[Export] public float SegmentSpacing
	{
		get => _segmentSpacing;
		set => _segmentSpacing = Mathf.Max(value, 0.0f);
	}
	[Export] public float WheelRadius { get; set; } = 25.0f;
	[Export] public float RingRadius
	{
		get => _ringRadius;
		set => _ringRadius = Mathf.Max(value, 0.0f);
	}
	[Export] public float RingThickness
	{
		get => _ringThickness;
		set => _ringThickness = Mathf.Max(value, 0.0f);
	}
	[Export] public int WheelResolution
	{
		get => _wheelResolution;
		set => _wheelResolution = Mathf.Clamp(value, 3, 256);
	}
	[Export] public Color DefaultRingColor { get; set; } = new Color(0.373f, 0.373f, 0.373f);
	[Export] public Color WheelDefaultColor { get; set; } = new Color(0.373f, 0.373f, 0.373f);
	[Export] public Array<SelectionWheelSlot> WheelSlots { get; set; } = new Array<SelectionWheelSlot>();

	[ExportGroup("Node Refs")]
	[Export] public Polygon2D WheelPolygon { get; set; }
	[Export] public Polygon2D RingPolygon { get; set; }

    public override string[] _GetConfigurationWarnings()
    {
		string[] baseWarnings = base._GetConfigurationWarnings() ?? System.Array.Empty<string>();
        Array<string> warnings = new Array<string>(baseWarnings);

		if (string.IsNullOrEmpty(SceneFilePath))
		{
			warnings.Add("Instance as scene.");
		}

		return warnings.ToArray<string>();
    }

    public override void _Ready()
    {
        LockWheelToCenter();
		Update();
    }

    public override void _ExitTree()
    {
        
    }

    public override void _Process(double delta)
    {
        LockWheelToCenter();
		Update();
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

		// Update polygon shape renderers.
		DrawCircle();
		DrawRing();

		// Update polygon object transforms.
		RingPolygon.Rotation = WheelRotation;

		// Update colors.
		WheelPolygon.Color = WheelDefaultColor;
		RingPolygon.Color = DefaultRingColor;
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
	/// Each WheelSlots float is an angular weight; spans are normalized around the ring.
	private void DrawRing()
	{
		if (RingPolygon is null)
		{
			throw new InvalidOperationException();
		}

		float[] weights = GetSegmentWeights();
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

		float totalWeight = 0.0f;
		for (int i = 0; i < segments; i++)
		{
			totalWeight += weights[i];
		}

		float innerRadius = radius;
		float outerRadius = radius + thickness;
		float baseGap = segments == 1 ? 0.0f : Mathf.DegToRad(SegmentSpacing);

		float[] segmentSpans = new float[segments];
		float[] gaps = new float[segments];
		float[] arcSpans = new float[segments];
		int[] steps = new int[segments];
		int totalPointPairs = 0;

		for (int i = 0; i < segments; i++)
		{
			segmentSpans[i] = weights[i] / totalWeight * Mathf.Tau;
			gaps[i] = Mathf.Min(baseGap, segmentSpans[i] * 0.95f);
			arcSpans[i] = segmentSpans[i] - gaps[i];
			steps[i] = Mathf.Max(2, Mathf.CeilToInt(resolution * (segmentSpans[i] / Mathf.Tau)));
			totalPointPairs += steps[i] + 1;
		}

		Vector2[] vertices = new Vector2[totalPointPairs * 2];
		Godot.Collections.Array triangles = new Godot.Collections.Array();

		int vertexCursor = 0;
		float angleCursor = 0.0f;

		for (int segment = 0; segment < segments; segment++)
		{
			int stepsPerSegment = steps[segment];
			int pointsPerSegment = stepsPerSegment + 1;
			float startAngle = angleCursor + gaps[segment] * 0.5f;
			int outerBase = vertexCursor;
			int innerBase = vertexCursor + pointsPerSegment;

			for (int i = 0; i < pointsPerSegment; i++)
			{
				float angle = startAngle + arcSpans[segment] * i / stepsPerSegment;
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

			vertexCursor += pointsPerSegment * 2;
			angleCursor += segmentSpans[segment];
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

	/// Returns the count of non null elements in the slots array, or 1 if the array is empty or all null.
	private int GetSegmentCount()
	{
		if (WheelSlots is null || WheelSlots.Count == 0 || IsAllNull(WheelSlots))
		{
			return 1;
		}

		int count = 0;
		foreach (SelectionWheelSlot slot in WheelSlots)
		{
			if (slot is not null && slot is SelectionWheelSlot)
			{
				count++;
			}
		}

		return count;
	}

	/// Angular weights from WheelSlots values. Empty or all-zero falls back to equal shares.
	private float[] GetSegmentWeights()
	{
		if (WheelSlots is null || WheelSlots.Count == 0 || IsAllNull(WheelSlots))
		{
			return new float[] { 1.0f };
		}

		float[] weights = new float[GetSegmentCount()];
		int writeIndex = 0;
		float total = 0.0f;

		for (int i = 0; i < WheelSlots.Count; i++)
		{
			SelectionWheelSlot slot = WheelSlots[i];
			if (slot is null)
			{
				continue;
			}

			weights[writeIndex++] = slot.SpaceBias;
			total += slot.SpaceBias;
		}

		if (Mathf.IsZeroApprox(total))
		{
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = 1.0f;
			}
		}

		return weights;
	}

	/// Returns true if every element in arr is null, and false otherwise.
	private static bool IsAllNull(Array<SelectionWheelSlot> arr)
	{
		if (arr is null)
		{
			return true;
		}

		foreach (SelectionWheelSlot item in arr)
		{
			if (item is not null)
			{
				return false;
			}
		}

		return true;
	}

	// private void _on_selection_wheel_resource_changed()
	// {
	// 	Update();
	// }
}
