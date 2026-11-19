using Godot;

[GlobalClass, Tool]
public partial class SelectionWheelSegment : Resource
{
    private float _spaceBias = 1.0f;
    private Color _color = new Color(0.373f, 0.373f, 0.373f);

    [Export] public float SpaceBias
    {
        get => _spaceBias;
        set
        {
            EmitSignal(SignalName.Changed);
            _spaceBias = Mathf.Max(0.0f, value);
        }
    }

    [Export] public Color Color
    {
        get => _color;
        set
        {
            EmitSignal(SignalName.Changed);
            _color = value;
        }
    }
}
