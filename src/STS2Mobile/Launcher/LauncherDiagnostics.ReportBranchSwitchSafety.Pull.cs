using System.IO;
using System.Text;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendManualPullEvidence(
        StringBuilder sb,
        string dataDir,
        string selectedBranch
    )
    {
        sb.AppendLine($"Manual Pull evidence marker filename: {LauncherCloudSyncEvidence.LastManualPullMarkerFileName}");
        sb.AppendLine($"Manual Pull evidence marker path: {LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir)}");
        sb.AppendLine($"Manual Pull evidence marker present: {BoolText(File.Exists(LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir)))}");
        sb.AppendLine($"Manual Pull evidence UTC: {LauncherCloudSyncEvidence.LastManualPullUtc(dataDir)}");
        sb.AppendLine($"Manual Pull evidence UTC parseable: {BoolText(LauncherCloudSyncEvidence.LastManualPullUtcParseable(dataDir))}");
        sb.AppendLine($"Manual Pull evidence selected branch: {LauncherCloudSyncEvidence.LastManualPullSelectedBranch(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected branch selection kind: {LauncherCloudSyncEvidence.LastManualPullSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selector mode: {LauncherCloudSyncEvidence.LastManualPullSelectorMode(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version: {LauncherCloudSyncEvidence.LastManualPullSelectedVersion(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version slot kind: {LauncherCloudSyncEvidence.LastManualPullSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version slot directory: {LauncherCloudSyncEvidence.LastManualPullSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Manual Pull completion flag recorded: {BoolText(LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Pull completed before Push: {BoolText(LauncherCloudSyncEvidence.LastManualPullBeforePushCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Pull evidence is after branch switch: {BoolText(LauncherCloudSyncEvidence.LastManualPullIsAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Manual Pull evidence matches selected branch: {BoolText(LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(dataDir, selectedBranch))}");
        sb.AppendLine($"Manual Pull completed after branch switch for selected version: {BoolText(LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(dataDir, selectedBranch))}");
        AppendModdedSaveSeedEvidence(sb, dataDir);
    }

    private static void AppendModdedSaveSeedEvidence(StringBuilder sb, string dataDir)
    {
        var path = AppPaths.ManualPullModdedSaveSeedEvidencePath(dataDir);
        sb.AppendLine($"Manual Pull modded-save provenance path: {path}");
        sb.AppendLine($"Manual Pull modded-save provenance present: {BoolText(File.Exists(path))}");
        if (!File.Exists(path))
            return;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            sb.AppendLine($"Manual Pull modded-save policy: {ReadString(root, "policy")}");
            sb.AppendLine($"Manual Pull modded-save completed UTC: {ReadString(root, "completedAtUtc")}");
            sb.AppendLine($"Manual Pull cloud-authoritative namespaces: {ArrayLength(root, "cloudAuthoritativeNamespaces")}");
            sb.AppendLine($"Manual Pull seeded modded files: {ArrayLength(root, "seededCopies")}");
            sb.AppendLine($"Manual Pull private modded backups: {ArrayLength(root, "privateBackupPaths")}");
            sb.AppendLine($"Manual Pull skipped seed sources: {ArrayLength(root, "skippedSeedSources")}");
            sb.AppendLine($"Manual Pull modded-save errors: {ArrayLength(root, "errors")}");
            sb.AppendLine($"Manual Pull modded-save Steam Cloud Push performed: {ReadBool(root, "steamCloudPushPerformed")}");
        }
        catch (System.Exception ex)
        {
            sb.AppendLine($"Manual Pull modded-save provenance parse error: {ex.Message}");
        }
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static int ArrayLength(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;

    private static string ReadBool(JsonElement root, string name)
        => root.TryGetProperty(name, out var value)
            && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                ? BoolText(value.GetBoolean())
                : "unknown";
}
