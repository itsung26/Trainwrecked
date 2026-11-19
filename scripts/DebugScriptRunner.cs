using System;
using Godot;
using Godot.Collections;

[GlobalClass, Tool]
public partial class DebugScriptRunner : Node
{
    [ExportToolButton("Run")]
    public Callable RunButton => Callable.From(Run);

    public virtual void Run()
    {
    }

    public override void _Process(double delta)
    {
    }
    
}