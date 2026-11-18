using System;
using Godot;
using Godot.Collections;

[GlobalClass, Tool]
public partial class DebugScriptRunner : Node
{
    [ExportToolButton("Run")]
    public Callable RunButton => Callable.From(Run);

    [Export] public Array<SelectionWheelSlot> TestList { get; set; }

    public virtual void Run()
    {
        Debug.Log(TestList);

        foreach (SelectionWheelSlot e in TestList)
        {
            Debug.Log(e is SelectionWheelSlot);
        }
    }
}