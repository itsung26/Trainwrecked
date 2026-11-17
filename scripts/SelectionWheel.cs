using Godot;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class SelectionWheel : Control
{
	#region Backing Fields
	private float _wheelRadius = 25.0f;
	private float _ringRadius = 25.0f;
	private float _ringThickness = 50.0f;
	private int _wheelResolution = 32;
	private Color _ringColor = Colors.White;
	private Color _wheelColor = Colors.White;
	private Dictionary<Resource, float> _wheelSlots = new();
	#endregion

	#region Public Properties
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

		// Update shapes.
		DrawCircle(WheelRadius, WheelResolution);
		DrawRing(RingRadius, RingThickness, WheelResolution);

		// Update colors.
		WheelPolygon.Color = WheelColor;
		RingPolygon.Color = RingColor;
	}

	private void DrawCircle(float radius, int resolution)
	{
		resolution = Mathf.Clamp(resolution, 3, 256);

		if (Mathf.IsEqualApprox(radius, 0.0f))
		{
			WheelPolygon.Polygon = System.Array.Empty<Vector2>();
			return;
		}

		Vector2[] circle = new Vector2[resolution];
		float angle = 0.0f;

		for (int i = 0; i < circle.Length; i++)
		{
			Vector2 point = ToRectangular(new Vector2(radius, angle));
			circle[i] = point;
			angle += (2.0f * Mathf.Pi) / resolution;
		}

		WheelPolygon.Polygon = circle;
	}

	/// Builds the ring as explicit triangles between the outer and inner circles.
	private void DrawRing(float radius, float thickness, int resolution)
	{
		if (RingPolygon is null)
		{
			throw new InvalidOperationException();
		}
		
		resolution = Mathf.Clamp(resolution, 3, 256);
		radius = Mathf.Max(radius, 0.0f);
		thickness = Mathf.Max(thickness, 0.0f);

		if (Mathf.IsZeroApprox(thickness))
		{
			RingPolygon.Polygon = System.Array.Empty<Vector2>();
			RingPolygon.Polygons = new Godot.Collections.Array();
			return;
		}

		float innerRadius = radius;
		float outerRadius = radius + thickness;

		Vector2[] vertices = new Vector2[resolution * 2];
		Godot.Collections.Array triangles = new();

		for (int i = 0; i < resolution; i++)
		{
			float angle = Mathf.Tau * i / resolution;
			Vector2 direction = Vector2.FromAngle(angle);

			vertices[i] = direction * outerRadius;
			vertices[i + resolution] = direction * innerRadius;
		}

		for (int i = 0; i < resolution; i++)
		{
			int next = (i + 1) % resolution;

			triangles.Add(new int[]
			{
				i, next, i + resolution
			});

			triangles.Add(new int[]
			{
				next, next + resolution, i + resolution
			});
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
}
