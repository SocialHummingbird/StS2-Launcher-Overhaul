namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    internal string Problem
    {
        get
        {
            if (Passed)
                return null;
            return "Game preparation failed for the selected branch. Repair selected branch, then try again. If it happens again, create a new support report.";
        }
    }
}
