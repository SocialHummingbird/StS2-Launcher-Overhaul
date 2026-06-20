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
            if (!Required)
                return null;
            if (!Exists)
                return "Selected game version has no Android patch compatibility validation evidence.";
            if (!Readable)
                return $"Selected game version has unreadable Android patch validation evidence ({Detail}).";
            if (!BranchMatches)
                return "Selected game version has Android patch validation evidence for a different Steam branch.";
            if (string.IsNullOrWhiteSpace(ValidatedPckSha256))
                return "Selected game version has Android patch validation evidence that does not declare the validated PCK.";
            if (!PckMatches)
                return "Selected game version has Android patch validation evidence for a different PCK.";
            if (string.IsNullOrWhiteSpace(ValidatedSourceAssemblySha256))
                return "Selected game version has Android patch validation evidence that does not declare the validated game-code assembly.";
            if (!SourceAssemblyMatches)
                return "Selected game version has Android patch validation evidence for a different game-code assembly.";

            return $"Selected game version failed Android patch compatibility validation ({Status}).";
        }
    }
}
