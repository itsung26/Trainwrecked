using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// Autoload that parses Godot user command-line arguments and exposes
/// typed launch flags for multiplayer startup.
/// </summary>
/// <remarks>
/// Reads <see cref="OS.GetCmdlineUserArgs"/> (values after <c>--</c>) in the constructor.
/// This type only sets flags; <see cref="NetworkManager"/> applies them in
/// <see cref="NetworkManager._Ready"/> via <see cref="NetworkManager.StartServer"/> /
/// <see cref="NetworkManager.StartClient"/>.
/// <para>
/// Supported flags:
/// <list type="bullet">
/// <item><c>--host</c> — set <see cref="HasHostFlag"/>.</item>
/// <item><c>--join=ADDRESS</c> — set <see cref="HasJoinFlag"/> and <see cref="JoinFlagAddress"/> when ADDRESS is a dotted IPv4.</item>
/// <item><c>--join=self</c> — join using <see cref="NetworkManager.GetPublicSubnetIpv4"/>.</item>
/// </list>
/// </para>
/// <para>
/// Invalid <c>--join</c> usage (missing value, empty value, or non-matching IP text) clears all flags.
/// <c>--host</c> and <c>--join</c> are mutually exclusive for a given parse; <c>--host</c> wins when both are present.
/// </para>
/// </remarks>
public partial class RunArguments : Node
{
    /// <summary>
    /// <see langword="true"/> when launch args request hosting a session.
    /// </summary>
    public static bool HasHostFlag { get; private set; } = false;

    /// <summary>
    /// <see langword="true"/> when launch args request joining a session.
    /// </summary>
    public static bool HasJoinFlag { get; private set; } = false;

    public static bool HasIgnoreAllFlag { get; private set; } = false;

    /// <summary>
    /// IPv4 address to join when <see cref="HasJoinFlag"/> is set; otherwise empty.
    /// </summary>
    public static string JoinFlagAddress { get; private set; } = "";


    private string[] _rawArgs;

    /// <summary>
    /// Parses <see cref="OS.GetCmdlineUserArgs"/> into the static flag properties.
    /// </summary>
    public RunArguments()
    {
        _rawArgs = OS.GetCmdlineUserArgs();
        ParseRunArgs();
    }

    /// <summary>
    /// Parses <see cref="_rawArgs"/> into <see cref="HasHostFlag"/>,
    /// <see cref="HasJoinFlag"/>, and <see cref="JoinFlagAddress"/>.
    /// </summary>
    /// <remarks>
    /// Unnamed <c>--host</c> selects host mode. Named <c>--join=VALUE</c> selects join mode
    /// when VALUE is <c>self</c> or a dotted IPv4.
    /// </remarks>
    public void ParseRunArgs()
    {
        Array<string> argumentsNamed = GetNamed();
        Array<string> argumentsUnnamed = GetUnnamed();

        if (argumentsUnnamed.Contains("--ignoreall"))
        {
            HasIgnoreAllFlag = true;
            return;
        }

        if (argumentsNamed.Count == 0 && argumentsUnnamed.Count == 0)
        {
            return;
        }

        if (argumentsUnnamed.Contains("--host"))
        {
            HasHostFlag = true;
            HasJoinFlag = false;
            JoinFlagAddress = "";
            return;
        }

        if (!TryGetNamedArgument("--join", out string joinValue))
        {
            return;
        }

        if (joinValue == "self")
        {
            HasHostFlag = false;
            HasJoinFlag = true;
            JoinFlagAddress = NetworkManager.GetPublicSubnetIpv4();
            return;
        }

        if (ValidateIpText(joinValue))
        {
            HasHostFlag = false;
            HasJoinFlag = true;
            JoinFlagAddress = joinValue;
            return;
        }

        HasHostFlag = false;
        HasJoinFlag = false;
        JoinFlagAddress = "";
    }

    /// <summary>
    /// Returns a copy of the user command-line tokens stored by <see cref="ParseRunArgs"/>.
    /// </summary>
    /// <returns>
    /// A new array of every token in <see cref="_rawArgs"/>, in the same order they were passed.
    /// </returns>
    /// <remarks>
    /// Does not re-read <see cref="OS.GetCmdlineUserArgs"/>. Mutating the returned array
    /// does not change <see cref="_rawArgs"/>.
    /// </remarks>
    public Array<string> GetRaw() {
        Array<string> ret = new Array<string>(_rawArgs);

        return ret;
    }

    /// <summary>
    /// Returns the raw tokens that use <c>name=value</c> form.
    /// </summary>
    /// <returns>
    /// A new array of arguments from <see cref="_rawArgs"/> that contain <c>=</c>,
    /// such as <c>--join=192.168.0.1</c>. Tokens without <c>=</c> are omitted.
    /// </returns>
    public Array<string> GetNamed() {
        Array<string> ret = new Array<string>();

        foreach (string argument in _rawArgs)
        {
            string[] parts = argument.Split('=');
            if (parts.Length == 2) {
                ret.Add(argument);
            }
        }

        return ret;
    }

    /// <summary>
    /// Returns the raw tokens that are not in <c>name=value</c> form.
    /// </summary>
    /// <returns>
    /// A new array of arguments from <see cref="_rawArgs"/> that do not contain <c>=</c>,
    /// such as <c>--host</c> or <c>--ignoreall</c>.
    /// </returns>
    public Array<string> GetUnnamed() {
        Array<string> ret = new Array<string>();

        foreach (string argument in _rawArgs) {
            if (!argument.Contains('=')) {
                ret.Add(argument);
            }
        }

        return ret;
    }

    /// <summary>
    /// Finds the first <c>name=value</c> token whose name equals <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The key to match, including leading dashes (for example <c>--join</c>).</param>
    /// <param name="value">
    /// The text after the first <c>=</c> when found; otherwise an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a matching named argument exists;
    /// otherwise <see langword="false"/>.
    /// </returns>
    private bool TryGetNamedArgument(string name, out string value)
    {
        foreach (string argument in GetNamed())
        {
            string[] parts = argument.Split('=', 2);
            if (parts.Length == 2 && parts[0] == name)
            {
                value = parts[1];
                return true;
            }
        }

        value = "";
        return false;
    }

    /// <summary>
    /// Returns whether <paramref name="newInput"/> looks like a dotted IPv4 address.
    /// </summary>
    /// <param name="newInput">Candidate address text; <see langword="null"/> is rejected.</param>
    /// <returns>
    /// <see langword="true"/> when the first 15 characters match <c>^\d{1,3}(\.\d{1,3}){3}$</c>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Truncates to 15 characters before matching. Does not enforce each octet in 0–255.
    /// </remarks>
    private bool ValidateIpText(string newInput)
	{
		if (newInput is null)
		{
			return false;
		}

		newInput = newInput.Substr(0, 15);

		// Regex regex = new Regex("[0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+", RegexOptions.IgnoreCase);
		return Regex.IsMatch(newInput, @"^\d{1,3}(\.\d{1,3}){3}$");
	}

}
