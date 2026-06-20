using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendGameRuntimeSlot(StringBuilder sb, string dataDir, string branch)
    {
        var slot = GameRuntimeSlot.Inspect(dataDir, branch);

        AppendRuntimeSlotSummary(sb, dataDir, branch, slot);
        AppendRuntimeSlotSelectedFiles(sb, slot);
        AppendRuntimePackEvidence(sb, slot);
        AppendPatchCompatibilityEvidence(sb, slot);
        AppendRuntimePatchValidationEvidence(sb, dataDir);
        AppendRuntimeCacheEvidence(sb, dataDir, branch);
        AppendRuntimePairingSummary(sb, slot);
    }
}
