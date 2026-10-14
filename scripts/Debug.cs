using Godot;
using System;
using Godot.Collections;

/// <summary>
/// Autoload for playtest logging and debug input.
/// </summary>
/// <remarks>
/// When <see cref="NetworkDebugWindowNamesEnabled"/> is <see langword="true"/>,
/// <see cref="_Ready"/> labels the window title from <see cref="RunArguments"/>.
/// <see cref="_Process"/> quits the game when the <c>Debug Quit</c> action is pressed.
/// The static log methods write to the Godot console.
/// </remarks>
public partial class Debug : Node
{
	/// <summary>
	/// Whether <see cref="_Ready"/> sets the window title from host and join flags.
	/// </summary>
	/// <remarks>
	/// Defaults to <see langword="true"/>. When <see langword="false"/>, the title is left unchanged.
	/// </remarks>
	public bool NetworkDebugWindowNamesEnabled { get; set; } = true;

	/// <summary>
	/// Labels the window <c>Trainwrecked (HOST)</c> or <c>Trainwrecked (CLIENT)</c>
	/// when <see cref="NetworkDebugWindowNamesEnabled"/> is <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// Uses <see cref="RunArguments.HasHostFlag"/> first, then <see cref="RunArguments.HasJoinFlag"/>.
	/// A process with neither flag keeps the existing title.
	/// </remarks>
	public override void _Ready()
	{
		if (NetworkDebugWindowNamesEnabled)
		{
			if (RunArguments.HasHostFlag)
			{
				GetWindow().Title = "Trainwrecked (HOST)";
			}
			else if (RunArguments.HasJoinFlag)
			{
				GetWindow().Title = "Trainwrecked (CLIENT)";
			}
		}
	}

	/// <summary>
	/// Quits the scene tree when the <c>Debug Quit</c> action is just pressed.
	/// </summary>
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Debug Quit"))
		{
			GetTree().Quit();
		}
	}

	/// <summary>
	/// Prints <paramref name="message"/> to the Godot console.
	/// </summary>
	/// <typeparam name="T">Type of the value to print.</typeparam>
	/// <param name="message">Value passed to <see cref="GD.Print(object)"/>. <see langword="null"/> is allowed.</param>
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
	public static String GenerateInstanceToString(Node WhichNode)
	{
		if (WhichNode is null)
		{
			return "";
		}
		return '<' + WhichNode.Name + '#' + WhichNode.GetInstanceId() + '>';
	}
}
