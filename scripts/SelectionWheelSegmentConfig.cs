using Godot;

[GlobalClass, Tool]
public partial class SelectionWheelSegmentConfig : Resource
{
    private float _colorChangeSpeed;

    [Export] public Color NormalColor { get; set; } = Colors.Gray;
    [Export] public Color SelectionColor { get; set; } = Colors.LightYellow;
    [Export] public float ColorChangeSpeed
    {
        get => _colorChangeSpeed;
        set
        {
            _colorChangeSpeed = Mathf.Max(0.001f, value);
        }
    }
    /// <summary>
    /// If null, the normal colors
    /// </summary>
    [Export] public ShaderMaterial CustomShaderOverrideMaterial { get; set; }
}