using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private static bool MatchesBranch(string declaredBranch, string expectedBranch)
        => !string.IsNullOrWhiteSpace(declaredBranch)
        && string.Equals(
            SteamGameBranch.Normalize(declaredBranch),
            SteamGameBranch.Normalize(expectedBranch),
            StringComparison.OrdinalIgnoreCase
        );

    private static bool MatchesDeclared(string declaredValue, string actualValue)
        => !string.IsNullOrWhiteSpace(declaredValue)
        && !string.IsNullOrWhiteSpace(actualValue)
        && !actualValue.StartsWith("<", StringComparison.Ordinal)
        && string.Equals(declaredValue, actualValue, StringComparison.OrdinalIgnoreCase);
}
