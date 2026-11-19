using Godot;
using Godot.Collections;
using System;

public partial class ObjectCastingTestScene : Node2D
{
	[Export] Array<SelectionWheelSegment> testArr = new Array<SelectionWheelSegment>();
	public override void _Ready()
	{
	}

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("Debug Action"))
		{
			testArr.RemoveAt(testArr.Count - 1);
		}
    }

	public override void _Process(double delta)
	{
		Debug.Log(IsAllNull(testArr));
	}

	/// Returns true if every element in arr is null, and false otherwise.
	private static bool IsAllNull(Array<SelectionWheelSegment> arr)
	{
		if (arr is null)
		{
			return true;
		}

		foreach (SelectionWheelSegment item in arr)
		{
			if (item is not null)
			{
				return false;
			}
		}

		return true;
	}

}
