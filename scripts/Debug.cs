using Godot;
using System;
using Godot.Collections;
using System.Collections.Generic;

/// <summary>
/// Autoload for playtest logging and debug input.
/// </summary>
public partial class Debug : Node
{
	public override void _Ready()
	{
		if (RunArguments.HasNameWindowFlag)
		{
			Window window = GetWindow();
			window.Title = RunArguments.WindowName;

			Vector2I screenPosition = DisplayServer.ScreenGetPosition(window.CurrentScreen);
			Vector2I screenSize = DisplayServer.ScreenGetSize(window.CurrentScreen);

			if (RunArguments.WindowName == "Host")
			{
				window.Position = screenPosition;
			}
			else if (RunArguments.WindowName == "Client")
			{
				window.Position = screenPosition + screenSize - window.Size;
			}
		}
	}

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("Debug Action"))
		{
			TrainConsist train = GetTree().CurrentScene.GetNode<TrainConsist>("TestTrain");
			train.Split(2);
			// train.Cars[2].QueueFree();
		}
    }

	/// <summary>
	/// Quits the scene tree when the <c>Debug Quit</c> action is just pressed.
	/// </summary>
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Debug Quit"))
		{
			GetWindow().EmitSignal(Window.SignalName.CloseRequested);
		}

		if (Input.IsActionJustPressed("Debug Action"))
		{
		}
	}

    /// <summary>
    /// Prints <paramref name="message"/> to the Godot console.
    /// </summary>
    /// <typeparam name="T">Type of the value to print.</typeparam>
    /// <param name="message">Value passed to <see cref="GD.Print(object[])"/>. <see langword="null"/> is allowed.</param>
    public static void Log<T>(T message)
	{
		GD.Print(message);
	}

	/// <summary>
	/// Prints a C# array to the Godot console as a Godot array.
	/// </summary>
	/// <typeparam name="T">Element type. Must be a Variant-compatible type.</typeparam>
	/// <param name="systemArrayMessage">
	/// Array copied into a <see cref="Godot.Collections.Array{T}"/> before printing. Must not be <see langword="null"/>.
	/// </param>
	public static void Log<[MustBeVariant] T>(T[] systemArrayMessage)
	{
		Array<T> godotArray = new Array<T>(systemArrayMessage);
		Log(godotArray);
	}

	/// <summary>
	/// Prints a C# list to the Godot console as a Godot array.
	/// </summary>
	/// <typeparam name="T">Element type. Must be a Variant-compatible type.</typeparam>
	/// <param name="systemListMessage">
	/// List copied into a <see cref="Godot.Collections.Array{T}"/> before printing. Must not be <see langword="null"/>.
	/// </param>
	public static void Log<[MustBeVariant] T>(List<T> systemListMessage)
	{
		Array<T> godotArray = new Array<T>(systemListMessage);
		Log(godotArray);
	}

	/// <summary>
	/// Prints a C# dictionary to the Godot console as a Godot dictionary.
	/// </summary>
	/// <typeparam name="T">Key type. Must be a Variant-compatible type.</typeparam>
	/// <typeparam name="E">Value type. Must be a Variant-compatible type.</typeparam>
	/// <param name="dictionary">
	/// Dictionary copied into a <see cref="Godot.Collections.Dictionary{TKey, TValue}"/> before printing.
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <remarks>
	/// Logging format example: <c>{ "health": 100, "mana": 50, "level": 3 }</c>.
	/// </remarks>
	public static void Log<[MustBeVariant] T, [MustBeVariant] E>(System.Collections.Generic.Dictionary<T, E> dictionary)
	{
		Log(new Godot.Collections.Dictionary<T, E>(dictionary));
	}

	/// <summary>
	/// Prints <paramref name="message"/> to the Godot console in <paramref name="color"/>.
	/// </summary>
	/// <typeparam name="T">Type of the value to print.</typeparam>
	/// <param name="message">
	/// Value converted with <see cref="object.ToString"/>. <see langword="null"/> prints the text <c>null</c>.
	/// </param>
	/// <param name="color">
	/// RGB color applied with BBCode. Alpha is ignored.
	/// </param>
	/// <remarks>
	/// Output goes through <see cref="GD.PrintRich(string)"/>. Brackets in the text are parsed as BBCode.
	/// </remarks>
	public static void LogColored<T>(T message, Color color)
	{
		string text = message is null ? "null" : message.ToString();
		GD.PrintRich($"[color=#{color.ToHtml(false)}]{text}[/color]");
	}

	/// <summary>
	/// Prints a C# array to the Godot console in <paramref name="color"/>.
	/// </summary>
	/// <typeparam name="T">Element type. Must be a Variant-compatible type.</typeparam>
	/// <param name="systemArrayMessage">
	/// Array copied into a <see cref="Godot.Collections.Array{T}"/> before printing. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="color">RGB color applied with BBCode. Alpha is ignored.</param>
	/// <remarks>
	/// The printed text is the Godot array's string form, so its brackets are parsed as BBCode
	/// the same way as <see cref="LogColored{T}(T, Color)"/>.
	/// </remarks>
	public static void LogColored<[MustBeVariant] T>(T[] systemArrayMessage, Color color)
	{
		Array<T> godotArray = new Array<T>(systemArrayMessage);
		LogColored(godotArray, color);
	}

	/// <summary>
	/// Prints a C# list to the Godot console in <paramref name="color"/>.
	/// </summary>
	/// <typeparam name="T">Element type. Must be a Variant-compatible type.</typeparam>
	/// <param name="systemListMessage">
	/// List copied into a <see cref="Godot.Collections.Array{T}"/> before printing. Must not be <see langword="null"/>.
	/// </param>
	/// <param name="color">RGB color applied with BBCode. Alpha is ignored.</param>
	/// <remarks>
	/// The printed text is the Godot array's string form, so its brackets are parsed as BBCode
	/// the same way as <see cref="LogColored{T}(T, Color)"/>.
	/// </remarks>
	public static void LogColored<[MustBeVariant] T>(List<T> systemListMessage, Color color)
	{
		Array<T> godotArray = new Array<T>(systemListMessage);
		LogColored(godotArray, color);
	}

	/// <summary>
	/// Prints a C# dictionary to the Godot console in <paramref name="color"/>.
	/// </summary>
	/// <typeparam name="T">Key type. Must be a Variant-compatible type.</typeparam>
	/// <typeparam name="E">Value type. Must be a Variant-compatible type.</typeparam>
	/// <param name="dictionary">
	/// Dictionary copied into a <see cref="Godot.Collections.Dictionary{TKey, TValue}"/> before printing.
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <param name="color">RGB color applied with BBCode. Alpha is ignored.</param>
	/// <remarks>
	/// The printed text is the Godot dictionary's string form, so its brackets are parsed as BBCode
	/// the same way as <see cref="LogColored{T}(T, Color)"/>.
	/// </remarks>
	public static void LogColored<[MustBeVariant] T, [MustBeVariant] E>(System.Collections.Generic.Dictionary<T, E> dictionary, Color color)
	{
		LogColored(new Godot.Collections.Dictionary<T, E>(dictionary), color);
	}

	/// <summary>
	/// Pushes <paramref name="warning"/> to the Godot debugger as a warning.
	/// </summary>
	/// <typeparam name="T">Type of the warning value.</typeparam>
	/// <param name="warning">Value converted with <see cref="object.ToString"/>. Must not be <see langword="null"/>.</param>
	public static void LogWarn<T>(T warning)
	{
		GD.PushWarning(warning.ToString());
	}

	/// <summary>
	/// Pushes <paramref name="error"/> to the Godot debugger as an error.
	/// </summary>
	/// <typeparam name="T">Type of the error value.</typeparam>
	/// <param name="error">Value converted with <see cref="object.ToString"/>. Must not be <see langword="null"/>.</param>
	public static void LogError<T>(T error)
	{
		GD.PushError(error.ToString());
	}

	/// <summary>
	/// Returns a label for <paramref name="WhichNode"/> in the form <c>&lt;Name#InstanceId&gt;</c>.
	/// </summary>
	/// <param name="WhichNode">Node whose name and instance id are formatted.</param>
	/// <returns>
	/// An empty string when <paramref name="WhichNode"/> is <see langword="null"/>;
	/// otherwise <c>&lt;</c>, the node name, <c>#</c>, <see cref="GodotObject.GetInstanceId"/>, and <c>&gt;</c>.
	/// </returns>
	public static string GenerateInstanceToString(Node WhichNode)
	{
		if (WhichNode is null)
		{
			return "";
		}
		return '<' + WhichNode.Name + '#' + WhichNode.GetInstanceId() + '>';
	}
}
