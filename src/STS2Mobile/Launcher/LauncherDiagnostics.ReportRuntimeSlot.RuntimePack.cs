using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendRuntimePackEvidence(StringBuilder sb, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected runtime pack manifest path: {slot.RuntimePackManifestPath}");
        sb.AppendLine($"Selected runtime pack manifest present: {BoolText(slot.RuntimePackManifestExists)}");
        sb.AppendLine($"Selected runtime pack status: {slot.RuntimePack.Status}");
        sb.AppendLine($"Selected runtime pack usability status: {slot.RuntimePackUsabilityStatus}");
        sb.AppendLine($"Selected runtime pack usable: {BoolText(slot.RuntimePackUsable)}");
        sb.AppendLine($"Selected runtime pack id: {ValueOrMissing(slot.RuntimePack.PackId)}");
        sb.AppendLine($"Selected runtime pack source runtime slot ID: {ValueOrMissing(slot.RuntimePack.SourceRuntimeSlotId)}");
        sb.AppendLine($"Selected runtime pack source runtime slot ID matches selected runtime: {BoolText(slot.RuntimePackSlotIdMatches)}");
        sb.AppendLine($"Selected runtime pack source branch: {ValueOrMissing(slot.RuntimePack.SourceBranch)}");
        sb.AppendLine($"Selected runtime pack source branch matches selected: {BoolText(slot.RuntimePack.BranchMatches)}");
        sb.AppendLine($"Selected runtime pack source PCK SHA256: {ValueOrMissing(slot.RuntimePack.SourcePckSha256)}");
        sb.AppendLine($"Selected runtime pack source assembly SHA256: {ValueOrMissing(slot.RuntimePack.SourceAssemblySha256)}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll path: {slot.RuntimePack.AndroidAssemblyPath}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll exists: {BoolText(slot.RuntimePack.AndroidAssemblyExists)}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll SHA256: {ValueOrMissing(slot.RuntimePack.AndroidAssemblySha256)}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll actual SHA256: {slot.RuntimePack.ActualAndroidAssemblySha256}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll hash matches manifest: {BoolText(slot.RuntimePack.AndroidAssemblyHashMatches)}");
        sb.AppendLine($"Selected runtime pack patch-set version: {ValueOrMissing(slot.RuntimePack.PatchSetVersion)}");
        sb.AppendLine($"Selected runtime pack patch validation status: {ValueOrMissing(slot.RuntimePack.PatchValidationStatus)}");
        sb.AppendLine($"Selected runtime pack patch validation report: {ValueOrMissing(slot.RuntimePack.PatchValidationReport)}");
        sb.AppendLine($"Selected runtime pack validation mode: {ValueOrMissing(slot.RuntimePack.ValidationMode)}");
        sb.AppendLine($"Selected runtime pack validation surface version: {ValueOrMissing(slot.RuntimePack.ValidationSurfaceVersion)}");
        sb.AppendLine($"Selected runtime pack generated from clean directory: {BoolText(slot.RuntimePack.GeneratedFromCleanDirectory)}");
        sb.AppendLine($"Selected runtime pack support assemblies declared: {BoolText(slot.RuntimePack.SupportAssembliesDeclared)}");
        sb.AppendLine($"Selected runtime pack support assemblies: {ValueOrMissing(string.Join(", ", slot.RuntimePack.SupportAssemblies))}");
        sb.AppendLine($"Selected runtime pack support assembly hashes declared: {BoolText(slot.RuntimePack.SupportAssemblySha256Declared)}");
        sb.AppendLine($"Selected runtime pack support assembly hash count: {slot.RuntimePack.SupportAssemblySha256.Count}");
        sb.AppendLine($"Selected runtime pack checked symbol count: {slot.RuntimePack.CheckedSymbolCount}");
        sb.AppendLine($"Selected runtime pack present symbol count: {slot.RuntimePack.PresentSymbolCount}");
        sb.AppendLine($"Selected runtime pack missing symbol count: {slot.RuntimePack.MissingSymbolCount}");
        sb.AppendLine($"Selected runtime pack minimum launcher version: {ValueOrMissing(slot.RuntimePack.MinimumLauncherVersion)}");
    }
}
