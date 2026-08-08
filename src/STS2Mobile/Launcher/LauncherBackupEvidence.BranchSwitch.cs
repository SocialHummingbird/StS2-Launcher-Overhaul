using System;
using System.Globalization;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBackupEvidence
{
    private static bool HasBackupAfterBranchSwitch(string dataDir, string source)
    {
        if (!TryReadBranchSwitchUtc(dataDir, out var branchSwitchUtc))
            return false;

        foreach (var backupPath in EnumerateBackups(source))
        {
            if (TryReadBackupUtc(backupPath, source, out var backupUtc)
                && backupUtc >= branchSwitchUtc)
                return true;
        }

        return false;
    }

    private static bool TryReadBranchSwitchUtc(string dataDir, out DateTime utc)
        => DateTime.TryParse(
            LauncherBranchSwitchSafety.MarkerUtc(dataDir),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out utc
        );
}
