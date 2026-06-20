using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendPatchCompatibilityEvidence(StringBuilder sb, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected patch compatibility source: {slot.PatchCompatibility.Source}");
        sb.AppendLine($"Selected patch compatibility marker path: {ValueOrMissing(slot.PatchCompatibility.MarkerPath)}");
        sb.AppendLine($"Selected patch compatibility required: {BoolText(slot.PatchCompatibility.Required)}");
        sb.AppendLine($"Selected patch compatibility evidence present: {BoolText(slot.PatchCompatibility.Exists)}");
        sb.AppendLine($"Selected patch compatibility evidence readable: {BoolText(slot.PatchCompatibility.Readable)}");
        sb.AppendLine($"Selected patch compatibility status: {slot.PatchCompatibility.Status}");
        sb.AppendLine($"Selected patch compatibility detail: {ValueOrMissing(slot.PatchCompatibility.Detail)}");
        sb.AppendLine($"Selected patch compatibility validated branch: {ValueOrMissing(slot.PatchCompatibility.ValidatedBranch)}");
        sb.AppendLine($"Selected patch compatibility branch matches selected: {BoolText(slot.PatchCompatibility.BranchMatches)}");
        sb.AppendLine($"Selected patch compatibility validated PCK SHA256: {ValueOrMissing(slot.PatchCompatibility.ValidatedPckSha256)}");
        sb.AppendLine($"Selected patch compatibility PCK matches selected: {BoolText(slot.PatchCompatibility.PckMatches)}");
        sb.AppendLine($"Selected patch compatibility validated source assembly SHA256: {ValueOrMissing(slot.PatchCompatibility.ValidatedSourceAssemblySha256)}");
        sb.AppendLine($"Selected patch compatibility source assembly matches selected: {BoolText(slot.PatchCompatibility.SourceAssemblyMatches)}");
        sb.AppendLine($"Selected patch compatibility patch-set version: {ValueOrMissing(slot.PatchCompatibility.PatchSetVersion)}");
        sb.AppendLine($"Selected patch compatibility validation mode: {ValueOrMissing(slot.PatchCompatibility.ValidationMode)}");
        sb.AppendLine($"Selected patch compatibility validation surface version: {ValueOrMissing(slot.PatchCompatibility.ValidationSurfaceVersion)}");
        sb.AppendLine($"Selected patch compatibility required symbol count: {slot.PatchCompatibility.RequiredSymbolCount}");
        sb.AppendLine($"Selected patch compatibility checked symbol count: {slot.PatchCompatibility.CheckedSymbolCount}");
        sb.AppendLine($"Selected patch compatibility present symbol count: {slot.PatchCompatibility.PresentSymbolCount}");
        sb.AppendLine($"Selected patch compatibility missing symbol count: {slot.PatchCompatibility.MissingSymbolCount}");
        sb.AppendLine($"Selected patch compatible: {BoolText(slot.PatchCompatible)}");
    }

    private static void AppendRuntimePatchValidationEvidence(StringBuilder sb, string dataDir)
    {
        sb.AppendLine($"Runtime patch validation marker filename: {LauncherRuntimePatchValidationEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime patch validation marker path: {LauncherRuntimePatchValidationEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime patch validation marker present: {BoolText(LauncherRuntimePatchValidationEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Runtime patch validation UTC: {LauncherRuntimePatchValidationEvidence.Utc(dataDir)}");
        sb.AppendLine($"Runtime patch validation UTC parseable: {BoolText(LauncherRuntimePatchValidationEvidence.UtcParseable(dataDir))}");
        sb.AppendLine($"Runtime patch validation status: {LauncherRuntimePatchValidationEvidence.Status(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected branch: {LauncherRuntimePatchValidationEvidence.SelectedBranch(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected version: {LauncherRuntimePatchValidationEvidence.SelectedVersion(dataDir)}");
        sb.AppendLine($"Runtime patch validation runtime slot ID: {LauncherRuntimePatchValidationEvidence.RuntimeSlotId(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected PCK SHA256: {LauncherRuntimePatchValidationEvidence.SelectedPckSha256(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected source sts2.dll SHA256: {LauncherRuntimePatchValidationEvidence.SelectedSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime patch validation active Android sts2.dll SHA256: {LauncherRuntimePatchValidationEvidence.ActiveAndroidAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime patch validation runtime pack id: {ValueOrMissing(LauncherRuntimePatchValidationEvidence.RuntimePackId(dataDir))}");
        sb.AppendLine($"Runtime patch validation runtime pack status: {LauncherRuntimePatchValidationEvidence.RuntimePackStatus(dataDir)}");
        sb.AppendLine($"Runtime patch validation applied patch count: {LauncherRuntimePatchValidationEvidence.AppliedPatchCount(dataDir)}");
        sb.AppendLine($"Runtime patch validation failed patch count: {LauncherRuntimePatchValidationEvidence.FailedPatchCount(dataDir)}");
        sb.AppendLine($"Runtime patch validation total patch count: {LauncherRuntimePatchValidationEvidence.TotalPatchCount(dataDir)}");
        sb.AppendLine($"Runtime patch validation failure messages: {LauncherRuntimePatchValidationEvidence.FailureMessages(dataDir)}");
    }
}
