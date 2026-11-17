using Godot;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class SelectionWheel : Control
{
	#region Backing Fields
	private float _wheelRadius = 25.0f;
	private float _wheelThickness = 50.0f;
	private int _wheelResolution = 32;
	private Dictionary<float, Resource> _wheelSlots = new();
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

	[Export] public float WheelThickness
	{
		get => _wheelThickness;
		set
		{
			_wheelThickness = value;
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

	[Export] public Dictionary<float, Resource> WheelSlots
	{
		get => _wheelSlots;
		set
		{
			_wheelSlots = value;
			Update();
		}
	}
	#endregion

	[ExportGroup("Node Refs")]
	[Export] public Polygon2D WheelPolygon { get; set; }

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

		WheelPolygon.GlobalPosition = GetRect().GetCenter();
	}

	/// Redraws the wheel based on exported parameters.
	private void Update()
	{
		if (WheelPolygon is null)
		{
			Debug.LogWarn("WheelPolygon is null, returning.");
			return;
		}

		WheelPolygon.Polygon = DrawCircle(WheelRadius, WheelResolution);
	}

	private static Vector2[] DrawCircle(float radius, int resolution)
	{
		resolution = Mathf.Clamp(resolution, 3, 256);

		if (Mathf.IsEqualApprox(radius, 0.0f))
		{
			return new Vector2[0];
		}

		Vector2[] circle = new Vector2[resolution];
		float angle = 0.0f;

		for (int i = 0; i < circle.Length; i++)
		{
			Vector2 point = ToRectangular(new Vector2(radius, angle));
			circle[i] = point;
			angle += (2.0f * Mathf.Pi) / resolution;
		}

		return circle;
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
