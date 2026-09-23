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
/// Supported flags (mode must be <c>args[0]</c>):
/// <list type="bullet">
/// <item><c>--host</c> — set <see cref="HasHostFlag"/>.</item>
/// <item><c>--join ADDRESS</c> — set <see cref="HasJoinFlag"/> and <see cref="JoinFlagAddress"/> when ADDRESS is a dotted IPv4.</item>
/// <item><c>--join --self</c> — join using <see cref="NetworkManager.GetPublicSubnetIpv4"/>.</item>
/// </list>
/// </para>
/// <para>
/// Invalid <c>--join</c> usage (missing operand, or non-matching IP text) clears all flags.
/// <c>--host</c> and <c>--join</c> are mutually exclusive for a given parse.
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

    /// <summary>
    /// IPv4 address to join when <see cref="HasJoinFlag"/> is set; otherwise empty.
    /// </summary>
    public static string JoinFlagAddress { get; private set; } = "";

    /// <summary>
    /// Parses <see cref="OS.GetCmdlineUserArgs"/> into the static flag properties.
    /// </summary>
    public RunArguments()
    {
        ParseRunArgs(OS.GetCmdlineUserArgs());
    }

    /// <summary>
    /// Parses <paramref name="args"/> into <see cref="HasHostFlag"/>,
    /// <see cref="HasJoinFlag"/>, and <see cref="JoinFlagAddress"/>.
    /// </summary>
    /// <param name="args">User arguments from <see cref="OS.GetCmdlineUserArgs"/>.</param>
    /// <remarks>
    /// Only inspects <c>args[0]</c> as the mode flag. A second token is required for <c>--join</c>.
    /// </remarks>
    public void ParseRunArgs(string[] args)
    {
        Array<string>arguments = new Array<string>(args);

        // No arguments to parse.
        if (arguments.Count <= 0 || arguments.Contains("--ignoreall"))
        {
            return;
        }

        if (arguments[0] == "--host")
        {
            HasHostFlag = true;
            HasJoinFlag = false;
            JoinFlagAddress = "";
        }
        else if (arguments[0] == "--join")
        {
            // No second argument: invalid.
            if (arguments.Count < 2)
            {
                HasHostFlag = false;
                HasJoinFlag = false;
                JoinFlagAddress = "";
            }
            else
            {
                // Parse the self argument.
                if (arguments[1] == "--self")
                {
                    HasHostFlag = false;
                    HasJoinFlag = true;
                    JoinFlagAddress = NetworkManager.GetPublicSubnetIpv4();
                }
                // Else if argument is not --self
                else if (arguments[1] != "--self")
                {
                    // Is argument 2 a valid IP?
                    if (ValidateIpText(arguments[1]))
                    {
                        HasHostFlag = false;
                        HasJoinFlag = true;
                        JoinFlagAddress = arguments[1];
                    }
                    // If not, invalidate.
                    else
                    {
                        HasHostFlag = false;
                        HasJoinFlag = false;
                        JoinFlagAddress = "";
                    }
                }
            }
        }
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
