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
	[Export]
	public float SegmentSpacing
	{
		get => _segmentSpacing;
		set => _segmentSpacing = Mathf.Max(value, 0.0f);
	}
	[Export] public float WheelRadius { get; set; } = 25.0f;
	[Export]
	public float RingRadius
	{
		get => _ringRadius;
		set => _ringRadius = Mathf.Max(value, 0.0f);
	}
	[Export]
	public float RingThickness
	{
		get => _ringThickness;
		set => _ringThickness = Mathf.Max(value, 0.0f);
	}
	[Export]
	public int WheelResolution
	{
		get => _wheelResolution;
		set => _wheelResolution = Mathf.Clamp(value, 3, 256);
	}
	[Export] public Color WheelColor { get; set; } = new Color(0.373f, 0.373f, 0.373f);
    public NonNullableArray<SelectionWheelSegment> Segments => GetSegments();
	[Export] public SelectionWheelSegmentConfig SegmentConfiguration { get; set; }

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
		GetNode<Label>("Label").Text = GetHoveredSegment().Name;
		LockWheelToCenter();
		if (Updating)
		{
			if (Engine.GetProcessFrames() % 1 == 0)
			{
				Update();
			}
		}
	}

	private Vector2 GetMousePosPolar()
	{
		// Offset from wheel center in local pixels (not Size-normalized — that warps angles
		// on non-square controls). Flip Y so up is positive to match the mouse convention.
		Vector2 fromCenter = GetLocalMousePosition() - GetRect().GetCenter();
		fromCenter.Y = -fromCenter.Y;
		return ToPolar(fromCenter);
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

		// Update center circle color.
		WheelPolygon.Color = WheelColor;

		// Update currently hovered segment.
		SelectionWheelSegment hovered = GetHoveredSegment();
		if (hovered is not null)
		{
			foreach (var segment in Segments)
			{
				if (!ReferenceEquals(segment, hovered))
				{
					segment.Selected = false;
				}
				else
				{
					segment.Selected = true;
				}
			}
		}
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

	/// Builds a segmented ring onto each SelectionWheelSegment under Ring.
	/// Each segment's SpaceBias is an angular weight; spans are normalized around the ring.
	private void DrawRing()
	{
		if (Ring is null)
		{
			throw new InvalidOperationException();
		}

		// Segments are Ring children in tree order (clockwise around the wheel).
		NonNullableArray<SelectionWheelSegment> segments = Segments;
		int segmentCount = segments.Count;
		float[] weights = GetSegmentWeights();
		int resolution = WheelResolution;
		float radius = RingRadius;
		float thickness = RingThickness;

		// Nothing to draw: clear any leftover polygon data on existing segments.
		if (segmentCount == 0 || Mathf.IsZeroApprox(thickness))
		{
			for (int i = 0; i < segmentCount; i++)
			{
				segments[i].Polygon = System.Array.Empty<Vector2>();
				segments[i].Polygons = new Godot.Collections.Array();
			}
			return;
		}

		float totalWeight = 0.0f;
		for (int i = 0; i < segmentCount; i++)
		{
			totalWeight += weights[i];
		}

		float innerRadius = radius;
		float outerRadius = radius + thickness;
		// Single segment fills the full ring; gaps only make sense between neighbors.
		float baseGap = segmentCount == 1 ? 0.0f : Mathf.DegToRad(SegmentSpacing);

		float[] segmentSpans = new float[segmentCount];
		float[] gaps = new float[segmentCount];
		float[] arcSpans = new float[segmentCount];
		int[] steps = new int[segmentCount];

		// Convert weights into angular budgets, then carve out spacing from each budget.
		for (int i = 0; i < segmentCount; i++)
		{
			segmentSpans[i] = weights[i] / totalWeight * Mathf.Tau;
			// Cap gap so a tiny/zero-weight segment cannot invert into a negative arc.
			gaps[i] = Mathf.Min(baseGap, segmentSpans[i] * 0.95f);
			arcSpans[i] = segmentSpans[i] - gaps[i];
			// More resolution for larger wedges so curve quality stays roughly even.
			steps[i] = Mathf.Max(2, Mathf.CeilToInt(resolution * (segmentSpans[i] / Mathf.Tau)));
		}

		float angleCursor = 0.0f;

		for (int segment = 0; segment < segmentCount; segment++)
		{
			int stepsPerSegment = steps[segment];
			int pointsPerSegment = stepsPerSegment + 1;
			// Center the gap on the segment boundary so spacing is shared with neighbors.
			float startAngle = angleCursor + gaps[segment] * 0.5f;
			const int outerBase = 0;
			int innerBase = pointsPerSegment;

			// Vertex layout: [outer arc...][inner arc...], then triangulate as quads.
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

			segments[segment].Polygon = vertices;
			segments[segment].Polygons = triangles;

			angleCursor += segmentSpans[segment];
		}
	}

	public void AddSelectionWheelSegment(SelectionWheelSegment newSegment, int index = -1)
	{
		index = index == -1 ? Segments.Count - 1 : index;

		if (index < 0 || index >= Segments.Count)
		{
			throw new IndexOutOfRangeException();
		}


		Ring.AddChild(newSegment);
		Ring.MoveChild(newSegment, index);
	}

	public void RemoveSelectionWheelSegment(int index)
	{
		if (index < 0 || index >= Segments.Count)
		{
			throw new IndexOutOfRangeException();
		}

		Ring.GetChild(index).QueueFree();
	}

	public void MoveSelectionWheelSegment(int from, int to)
	{
		if (from < 0 || from >= Segments.Count)
		{
			throw new IndexOutOfRangeException();
		}

		if (to < 0 || to >= Segments.Count)
		{
			throw new IndexOutOfRangeException();
		}

		Ring.MoveChild(Segments[from], to);
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

	private int GetSegmentCount()
	{
		return Segments.Count;
	}

	/// Angular weights from each segment's SpaceBias. All-zero falls back to equal shares.
	private float[] GetSegmentWeights()
	{
		NonNullableArray<SelectionWheelSegment> segments = Segments;
		float[] weights = new float[segments.Count];
		float totalWeight = 0.0f;

		for (int i = 0; i < segments.Count; i++)
		{
			weights[i] = segments[i].SpaceBias;
			totalWeight += weights[i];
		}

		if (Mathf.IsZeroApprox(totalWeight))
		{
			for (int i = 0; i < weights.Length; i++)
			{
				weights[i] = 1.0f;
			}
		}

		return weights;
	}

	/// <summary>
	/// Returns the segment that the mouse is in the area of.
	/// Only the angle of the mouse's polar pos is taken into account.
	/// </summary>
	private SelectionWheelSegment GetHoveredSegment()
	{
		NonNullableArray<SelectionWheelSegment> segments = Segments;
		int segmentCount = segments.Count;
		if (segmentCount == 0)
		{
			return null;
		}

		// Mouse polar is Y-up; ring geometry uses Godot FromAngle (Y-down). Negate to match,
		// then undo WheelRotation so the angle is in ring-local space.
		float angle = Mathf.PosMod(-GetMousePosPolar().Y - WheelRotation, Mathf.Tau);

		float[] weights = GetSegmentWeights();
		float totalWeight = 0.0f;
		for (int i = 0; i < segmentCount; i++)
		{
			totalWeight += weights[i];
		}

		float angleCursor = 0.0f;
		for (int i = 0; i < segmentCount; i++)
		{
			float span = weights[i] / totalWeight * Mathf.Tau;
			if (angle >= angleCursor && angle < angleCursor + span)
			{
				return segments[i];
			}

			angleCursor += span;
		}

		// Angle landed exactly on Tau after PosMod edge cases — last segment owns the wrap.
		return segments[segmentCount - 1];
	}

	private NonNullableArray<SelectionWheelSegment> GetSegments()
	{
		Array<Node> children = Ring.GetChildren();
		NonNullableArray<SelectionWheelSegment> segments = new NonNullableArray<SelectionWheelSegment>();

		foreach (var child in children)
		{
			if (child is SelectionWheelSegment newSegment)
			{
				if (!newSegment.IsQueuedForDeletion())
				{
					segments.Add(newSegment);
				}
			}
		}

		return segments;
	}

	private void _on_ring_child_entered_tree(Node node)
	{
		if (node is SelectionWheelSegment segment)
		{
			Debug.Log(segment.Name);
		}
	}
}
