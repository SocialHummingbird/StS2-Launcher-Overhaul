namespace STS2Mobile.Launcher;

internal static partial class LauncherBackupEvidence
{
    private static int CountBackups(string source)
    {
        var count = 0;
        foreach (var _ in EnumerateBackups(source))
            count++;

        return count;
    }
}
