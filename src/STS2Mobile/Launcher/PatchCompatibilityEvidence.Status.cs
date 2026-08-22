using System;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private const string PassedStatus = "passed";

    internal bool Passed =>
        string.Equals(Status, PassedStatus, StringComparison.OrdinalIgnoreCase)
        && GameIdentityMatches;
}
