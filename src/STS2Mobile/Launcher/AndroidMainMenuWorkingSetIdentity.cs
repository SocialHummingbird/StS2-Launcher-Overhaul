using System;

namespace STS2Mobile.Launcher;

internal readonly struct AndroidMainMenuWorkingSetIdentity
{
    private const string Unknown = "<unknown>";

    private AndroidMainMenuWorkingSetIdentity(
        string branch,
        string pckIdentity,
        string modMode
    )
    {
        Branch = branch;
        PckIdentity = pckIdentity;
        ModMode = modMode;
    }

    internal string Branch { get; }
    internal string PckIdentity { get; }
    internal string ModMode { get; }
    internal string ResourceSetKey
        => Branch == Unknown || PckIdentity == Unknown || ModMode == Unknown
            ? string.Empty
            : $"{Branch}|{PckIdentity}|{ModMode}";

    internal static AndroidMainMenuWorkingSetIdentity Create(
        string branch,
        string pckSha256,
        string modMode
    )
        => new(
            CleanToken(branch),
            ShortHash(pckSha256),
            CleanToken(modMode)
        );

    internal static AndroidMainMenuWorkingSetIdentity Missing()
        => new(Unknown, Unknown, Unknown);

    private static string CleanToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Unknown;

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
            return Unknown;

        foreach (var character in trimmed)
        {
            if (!char.IsLetterOrDigit(character)
                && character is not '-' and not '_' and not '.')
            {
                return Unknown;
            }
        }

        return trimmed;
    }

    private static string ShortHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Unknown;

        var trimmed = value.Trim();
        if (trimmed.Length != 64)
            return Unknown;

        foreach (var character in trimmed)
        {
            if (!Uri.IsHexDigit(character))
                return Unknown;
        }

        return trimmed.Substring(0, 12).ToLowerInvariant();
    }
}
