using System;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    internal string Problem
    {
        get
        {
            if (Passed)
                return null;
            if (!Exists)
                return "Selected game version has no Android patch compatibility validation evidence.";
            if (!Readable)
                return $"Selected game version has unreadable Android patch validation evidence ({Detail}).";
            if (ValidatedGameIdentity == null)
                return "Selected game version has Android patch validation evidence without a complete game identity.";
            if (!GameIdentityMatches)
                return "Selected game version has Android patch validation evidence for a different game identity.";

            var detail = string.IsNullOrWhiteSpace(Detail)
                ? Status
                : $"{Status}: {Detail}";
            return $"Selected game version failed Android patch compatibility validation ({detail}).";
        }
    }
}
