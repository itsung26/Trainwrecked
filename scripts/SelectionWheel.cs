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

	[Export] public bool Updating { get; set; } = true;
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
	[Export] public Color WheelColor { get; set; } = new Color(0.373f, 0.373f, 0.373f);
	[Export] public Array<SelectionWheelSegment> WheelSlots { get; set; } = new Array<SelectionWheelSegment>();

	[ExportGroup("Node Refs")]
	[Export] public Polygon2D WheelPolygon { get; set; }
	[Export] public Node2D Ring { get; set; }

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
		if (Updating)
		{
			Update();
		}
    }

    public override void _Process(double delta)
    {
        LockWheelToCenter();
		if (Updating)
		{
			Update();
		}
    }

	/// Keeps the wheel in the center of this control's rectangle.
	private void LockWheelToCenter()
	{
		if (WheelPolygon is null || Ring is null)
		{
			throw new InvalidOperationException();
		}

		Vector2 center = GetRect().GetCenter();

		WheelPolygon.GlobalPosition = center;
		Ring.GlobalPosition = center;
	}

	/// Redraws the entire wheel based on exported parameters.
	/// Called every frame.
	private void Update()
	{
		if (WheelPolygon is null || Ring is null)
		{
			return;
		}

		// Update polygon shape renderers.
		DrawCircle();
		DrawRing();

		// Update polygon object transforms.
		Ring.Rotation = WheelRotation;

		// Update colors.
		WheelPolygon.Color = WheelColor;
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

	/// Builds a segmented ring as one Polygon2D child per segment under Ring.
	/// Each WheelSlots float is an angular weight; spans are normalized around the ring.
	private void DrawRing()
	{
		if (Ring is null)
		{
			throw new InvalidOperationException();
		}

		float[] weights = GetSegmentWeights();
		int segments = GetSegmentCount();
		int resolution = WheelResolution;
		float radius = RingRadius;
		float thickness = RingThickness;

		if (Mathf.IsZeroApprox(thickness))
		{
			SyncRingSegmentCount(0);
			return;
		}

		bool usingSlots = WheelSlots is not null && WheelSlots.Count > 0 && !IsAllNull(WheelSlots);
		int ringChildCount = usingSlots ? WheelSlots.Count : 1;
		SyncRingSegmentCount(ringChildCount);

		int[] slotIndices = new int[segments];
		if (usingSlots)
		{
			int writeIndex = 0;
			for (int i = 0; i < WheelSlots.Count; i++)
			{
				if (WheelSlots[i] is null)
				{
					ClearRingSegment(i);
					continue;
				}

				slotIndices[writeIndex++] = i;
			}
		}
		else
		{
			slotIndices[0] = 0;
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

		for (int i = 0; i < segments; i++)
		{
			segmentSpans[i] = weights[i] / totalWeight * Mathf.Tau;
			gaps[i] = Mathf.Min(baseGap, segmentSpans[i] * 0.95f);
			arcSpans[i] = segmentSpans[i] - gaps[i];
			steps[i] = Mathf.Max(2, Mathf.CeilToInt(resolution * (segmentSpans[i] / Mathf.Tau)));
		}

		float angleCursor = 0.0f;

		for (int segment = 0; segment < segments; segment++)
		{
			int stepsPerSegment = steps[segment];
			int pointsPerSegment = stepsPerSegment + 1;
			float startAngle = angleCursor + gaps[segment] * 0.5f;
			const int outerBase = 0;
			int innerBase = pointsPerSegment;

			Vector2[] vertices = new Vector2[pointsPerSegment * 2];
			Godot.Collections.Array triangles = new Godot.Collections.Array();

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

			Polygon2D segmentPolygon = GetRingSegment(slotIndices[segment]);
			segmentPolygon.Polygon = vertices;
			segmentPolygon.Polygons = triangles;

			angleCursor += segmentSpans[segment];
		}
	}

	/// Adds or removes Polygon2D children under Ring.
	/// Children are named by ring index: "0" .. "{count - 1}" matching WheelSlots indices.
	private void SyncRingSegmentCount(int segmentCount)
	{
		while (Ring.GetChildCount() > segmentCount)
		{
			Node child = Ring.GetChild(Ring.GetChildCount() - 1);
			Ring.RemoveChild(child);
			child.QueueFree();
		}

		while (Ring.GetChildCount() < segmentCount)
		{
			int index = Ring.GetChildCount();
			Polygon2D segment = new Polygon2D
			{
				Name = GetRingSegmentName(index)
			};
			Ring.AddChild(segment);
		}

		for (int i = 0; i < Ring.GetChildCount(); i++)
		{
			Ring.GetChild(i).Name = GetRingSegmentName(i);
		}
	}

	private static string GetRingSegmentName(int ringIndex) => ringIndex.ToString();

	private Polygon2D GetRingSegment(int ringIndex)
	{
		return Ring.GetNode<Polygon2D>(GetRingSegmentName(ringIndex));
	}

	private void ClearRingSegment(int ringIndex)
	{
		Polygon2D segment = GetRingSegment(ringIndex);
		segment.Polygon = System.Array.Empty<Vector2>();
		segment.Polygons = new Godot.Collections.Array();
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
		foreach (SelectionWheelSegment slot in WheelSlots)
		{
			if (slot is not null && slot is SelectionWheelSegment)
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
			SelectionWheelSegment slot = WheelSlots[i];
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
	private static bool IsAllNull(Array<SelectionWheelSegment> arr)
	{
		if (arr is null)
		{
			return true;
		}

		foreach (SelectionWheelSegment item in arr)
		{
			if (item is not null)
			{
				return false;
			}
		}

		return true;
	}

}
