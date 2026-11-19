using Godot;

[GlobalClass, Tool]
public partial class SelectionWheelSegment : Polygon2D
{
    private float _spaceBias = 1.0f;

    [Export] public float SpaceBias
    {
        get => _spaceBias;
        set => _spaceBias = Mathf.Max(0.0f, value);
    }
    [Export] public bool Selected { get; set; }
    [Export] public SelectionWheelSegmentConfig Configuration { get; set; }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }
        
        UpdateColor(delta);
    }

    private void UpdateColor(double delta)
    {
        if (Configuration is null)
        {
            Color = Colors.White;
            return;
        }

        Color selectedCol = Configuration.SelectionColor;
        Color regularCol = Configuration.NormalColor;
        float changeSpeed = Configuration.ColorChangeSpeed;
        if (Selected && Color != selectedCol)
        {
            Color = Color.Lerp(selectedCol, changeSpeed * (float)delta);
        }
        else if (!Selected && Color != regularCol)
        {
            Color = Color.Lerp(regularCol, changeSpeed * (float)delta);
        }
    }
}
