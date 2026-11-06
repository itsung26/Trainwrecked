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
/// <item><c>--ignoreall</c> — set <see cref="HasIgnoreAllFlag"/> and skip all other parsing.</item>
/// <item><c>--skipintro</c> — set <see cref="HasSkipIntroFlag"/>.</item>
/// <item><c>--host</c> — set <see cref="HasHostFlag"/>.</item>
/// <item><c>--join=ADDRESS</c> — set <see cref="HasJoinFlag"/> and <see cref="JoinFlagAddress"/> when ADDRESS is a dotted IPv4.</item>
/// <item><c>--join=self</c> — join using <see cref="NetworkManager.GetPublicSubnetIpv4"/>.</item>
/// <item><c>--window-name=TITLE</c> — set <see cref="HasNameWindowFlag"/> and <see cref="WindowName"/> to TITLE.</item>
/// </list>
/// </para>
/// <para>
/// Invalid <c>--join</c> usage (missing value, empty value, or non-matching IP text) clears host and join flags.
/// <c>--ignoreall</c> takes precedence over every other flag. <c>--host</c> and <c>--join</c> are mutually exclusive;
/// <c>--host</c> wins when both are present. <c>--window-name</c> and <c>--skipintro</c> may be combined with host or join.
/// </para>
/// </remarks>
public sealed partial class RunArguments : Node
{
    /// <summary>
    /// <see langword="true"/> when launch args request hosting a session.
    /// </summary>
    public static bool HasHostFlag { get; private set; } = false;

    /// <summary>
    /// <see langword="true"/> when launch args request joining a session.
    /// </summary>
    public static bool HasJoinFlag { get; private set; } = false;

    /// <summary>
    /// <see langword="true"/> when launch args request ignoring all other run flags.
    /// </summary>
    /// <remarks>
    /// Set by the unnamed token <c>--ignoreall</c>. When set during parse, host and join flags are left unset
    /// and no further argument handling runs.
    /// </remarks>
    public static bool HasIgnoreAllFlag { get; private set; } = false;

    /// <summary>
    /// <see langword="true"/> when launch args request skipping the intro.
    /// </summary>
    /// <remarks>
    /// Set by the unnamed token <c>--skipintro</c>. May be combined with host, join, and window-name flags.
    /// </remarks>
    public static bool HasSkipIntroFlag { get; private set; } = false;

    /// <summary>
    /// <see langword="true"/> when launch args include a non-empty <c>--window-name</c> value.
    /// </summary>
    public static bool HasNameWindowFlag { get; private set; } = false;

    /// <summary>
    /// Window title from <c>--window-name=TITLE</c> when <see cref="HasNameWindowFlag"/> is set; otherwise empty.
    /// </summary>
    public static string WindowName { get; private set; } = "";

    /// <summary>
    /// IPv4 address to join when <see cref="HasJoinFlag"/> is set; otherwise empty.
    /// </summary>
    public static string JoinFlagAddress { get; private set; } = "";

    /// <summary>
    /// Parses <see cref="OS.GetCmdlineUserArgs"/> into the static flag properties.
    /// </summary>
    public RunArguments()
    {
        ParseRunArgs();
    }

    /// <summary>
    /// Parses <see cref="OS.GetCmdlineUserArgs"/> into <see cref="HasIgnoreAllFlag"/>,
    /// <see cref="HasSkipIntroFlag"/>, <see cref="HasHostFlag"/>, <see cref="HasJoinFlag"/>,
    /// <see cref="JoinFlagAddress"/>, <see cref="HasNameWindowFlag"/>, and <see cref="WindowName"/>.
    /// </summary>
    /// <remarks>
    /// Unnamed <c>--ignoreall</c> sets <see cref="HasIgnoreAllFlag"/> and returns immediately.
    /// Unnamed <c>--skipintro</c> sets <see cref="HasSkipIntroFlag"/>.
    /// Named <c>--window-name=TITLE</c> sets the window title when TITLE is non-empty.
    /// Unnamed <c>--host</c> selects host mode. Named <c>--join=VALUE</c> selects join mode
    /// when VALUE is <c>self</c> or a dotted IPv4.
    /// </remarks>
    private static void ParseRunArgs()
    {
        Array<string> argumentsNamed = RunArguments.GetNamed();
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

        if (argumentsUnnamed.Contains("--skipintro"))
        {
            HasSkipIntroFlag = true;
        }

        if (TryGetNamedArgument("--window-name", out string windowName))
        {
            windowName = TrimSurroundingQuotes(windowName);
            if (windowName != "")
            {
                HasNameWindowFlag = true;
                WindowName = windowName;
            }
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
    /// Removes one matching pair of surrounding double quotes from <paramref name="value"/> when present.
    /// </summary>
    private static string TrimSurroundingQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    /// <summary>
    /// Returns a copy of the current user command-line tokens.
    /// </summary>
    /// <returns>
    /// A new array of every token from <see cref="OS.GetCmdlineUserArgs"/>, in the same order they were passed.
    /// </returns>
    /// <remarks>
    /// Re-reads <see cref="OS.GetCmdlineUserArgs"/> on each call. Mutating the returned array
    /// does not change the process argument list.
    /// </remarks>
    public static Array<string> GetRaw() {
        Array<string> ret = new Array<string>(OS.GetCmdlineUserArgs());

        return ret;
    }

    /// <summary>
    /// Returns the raw tokens that use exactly one <c>name=value</c> pair.
    /// </summary>
    /// <returns>
    /// A new array of arguments from <see cref="OS.GetCmdlineUserArgs"/> that split into exactly two parts on <c>=</c>,
    /// such as <c>--join=192.168.0.1</c>. Tokens without <c>=</c>, or with more than one <c>=</c>, are omitted.
    /// </returns>
    public static Array<string> GetNamed() {
        Array<string> ret = new Array<string>();

        foreach (string argument in OS.GetCmdlineUserArgs())
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
    /// A new array of arguments from <see cref="OS.GetCmdlineUserArgs"/> that do not contain <c>=</c>,
    /// such as <c>--host</c>, <c>--skipintro</c>, or <c>--ignoreall</c>.
    /// </returns>
    public static Array<string> GetUnnamed() {
        Array<string> ret = new Array<string>();

        foreach (string argument in OS.GetCmdlineUserArgs()) {
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
    private static bool TryGetNamedArgument(string name, out string value)
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
    private static bool ValidateIpText(string newInput)
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
