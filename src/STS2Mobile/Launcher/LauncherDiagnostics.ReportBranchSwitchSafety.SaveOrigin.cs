using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendCurrentLocalSaveEvidence(StringBuilder sb, string dataDir)
    {
        sb.AppendLine($"Current important Android local save evidence count: {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}");
        sb.AppendLine($"Current important Android local save evidence present: {BoolText(LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir))}");
    }

    private static void AppendSaveOriginEvidence(
        StringBuilder sb,
        string dataDir,
        string selectedBranch
    )
    {
        sb.AppendLine($"Android save-origin marker filename: {LauncherSaveOriginEvidence.MarkerFileName}");
        sb.AppendLine($"Android save-origin marker path: {LauncherSaveOriginEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Android save-origin marker present: {BoolText(LauncherSaveOriginEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Android save-origin UTC: {LauncherSaveOriginEvidence.OriginUtc(dataDir)}");
        sb.AppendLine($"Android save-origin UTC parseable: {BoolText(LauncherSaveOriginEvidence.OriginUtcParseable(dataDir))}");
        sb.AppendLine($"Android save-origin action: {LauncherSaveOriginEvidence.OriginAction(dataDir)}");
        sb.AppendLine($"Android save-origin selected branch: {LauncherSaveOriginEvidence.SelectedBranch(dataDir)}");
        sb.AppendLine($"Android save-origin selected version: {LauncherSaveOriginEvidence.SelectedVersion(dataDir)}");
        sb.AppendLine($"Android save-origin selected version slot kind: {LauncherSaveOriginEvidence.SelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Android save-origin selected version slot directory: {LauncherSaveOriginEvidence.SelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Android save-origin selected runtime slot ID: {LauncherSaveOriginEvidence.SelectedRuntimeSlotId(dataDir)}");
        sb.AppendLine($"Android save-origin selected PCK SHA256: {LauncherSaveOriginEvidence.SelectedPckSha256(dataDir)}");
        sb.AppendLine($"Android save-origin selected source sts2.dll SHA256: {LauncherSaveOriginEvidence.SelectedSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Android save-origin selected runtime playable at origin: {LauncherSaveOriginEvidence.SelectedRuntimePlayable(dataDir)}");
        sb.AppendLine($"Android save-origin selected runtime readiness problem at origin: {LauncherSaveOriginEvidence.SelectedRuntimeReadinessProblem(dataDir)}");
        sb.AppendLine($"Android save-origin important local save evidence count: {LauncherSaveOriginEvidence.ImportantLocalSaveEvidenceCount(dataDir)}");
        sb.AppendLine($"Android save-origin matches selected branch: {BoolText(LauncherSaveOriginEvidence.MatchesSelectedBranch(dataDir, selectedBranch))}");
        sb.AppendLine($"Android save-origin current selected runtime is playable: {BoolText(LauncherSaveOriginEvidence.SelectedRuntimeCurrentlyPlayable(dataDir, selectedBranch))}");
        sb.AppendLine($"Android save-origin selected runtime slot ID matches current runtime: {BoolText(LauncherSaveOriginEvidence.RuntimeSlotIdMatchesSelectedRuntime(dataDir, selectedBranch))}");
        sb.AppendLine($"Android save-origin selected PCK matches current runtime: {BoolText(LauncherSaveOriginEvidence.PckMatchesSelectedRuntime(dataDir, selectedBranch))}");
        sb.AppendLine($"Android save-origin selected source sts2.dll matches current runtime: {BoolText(LauncherSaveOriginEvidence.SourceAssemblyMatchesSelectedRuntime(dataDir, selectedBranch))}");
        sb.AppendLine($"Android local saves verified for selected branch: {BoolText(LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedBranch(dataDir, selectedBranch))}");
        sb.AppendLine($"Android local saves verified for selected runtime: {BoolText(LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(dataDir, selectedBranch))}");
        sb.AppendLine($"Baseline manual Push prerequisites satisfied: {BoolText(LauncherCloudSyncEvidence.BaselineManualPushPrerequisitesSatisfied(dataDir, selectedBranch))}");
    }
}
