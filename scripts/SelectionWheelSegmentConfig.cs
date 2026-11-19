using Godot;

[GlobalClass, Tool]
public partial class SelectionWheelSegmentConfig : Resource
{
    [Export] public Color NormalColor { get; set; }
    [Export] public Color SelectionColor { get; set; }
    [Export] public float ColorChangeSpeed { get; set; }
}