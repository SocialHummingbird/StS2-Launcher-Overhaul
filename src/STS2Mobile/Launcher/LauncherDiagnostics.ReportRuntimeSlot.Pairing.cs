using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendRuntimePairingSummary(StringBuilder sb, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected runtime pairing status: {slot.RuntimePairingStatus}");
        sb.AppendLine($"Selected runtime requires usable runtime pack: {BoolText(slot.RequiresRuntimePackOrPreparedCache)}");
        sb.AppendLine($"Selected runtime branch-matched Android runtime prepared: {BoolText(slot.BranchMatchedAndroidRuntimePrepared)}");
        sb.AppendLine($"Selected runtime compatible: {BoolText(slot.RuntimeCompatible)}");
        sb.AppendLine($"Selected runtime playable: {BoolText(slot.Playable)}");
    }
}
