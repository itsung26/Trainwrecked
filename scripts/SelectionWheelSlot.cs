using Godot;

[GlobalClass, Tool]
public partial class SelectionWheelSlot : Resource
{
    private float _spaceBias = 1.0f;

    [Export] public float SpaceBias
    {
        get => _spaceBias;
        set => _spaceBias = Mathf.Max(0.0f, value);
    }
}
