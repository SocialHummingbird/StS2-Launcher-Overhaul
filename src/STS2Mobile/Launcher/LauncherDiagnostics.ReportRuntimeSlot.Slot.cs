using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendRuntimeSlotSummary(StringBuilder sb, string dataDir, string branch, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected runtime slot branch: {slot.Branch}");
        sb.AppendLine($"Selected runtime slot display name: {slot.DisplayName}");
        sb.AppendLine($"Selected runtime slot kind: {slot.SlotKind}");
        sb.AppendLine($"Selected runtime slot directory: {slot.SlotDirectory}");
        sb.AppendLine($"Runtime slot evidence marker filename: {LauncherRuntimeSlotEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime slot evidence marker path: {LauncherRuntimeSlotEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime slot evidence marker present: {BoolText(LauncherRuntimeSlotEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Runtime slot evidence selected branch: {LauncherRuntimeSlotEvidence.Branch(dataDir)}");
        sb.AppendLine($"Runtime slot evidence selected branch matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.BranchMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence runtime slot ID: {LauncherRuntimeSlotEvidence.RuntimeSlotId(dataDir)}");
        sb.AppendLine($"Runtime slot evidence runtime slot ID matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.RuntimeSlotIdMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence selected PCK matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.PckMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence selected source sts2.dll matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.SourceAssemblyMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence files ready: {LauncherRuntimeSlotEvidence.FilesReady(dataDir)}");
        sb.AppendLine($"Runtime slot evidence readiness problem: {ValueOrMissing(LauncherRuntimeSlotEvidence.ReadinessProblem(dataDir))}");
        sb.AppendLine($"Runtime slot evidence runtime pack usability status: {LauncherRuntimeSlotEvidence.RuntimePackUsabilityStatus(dataDir)}");
        sb.AppendLine($"Runtime slot evidence patch compatibility status: {LauncherRuntimeSlotEvidence.PatchCompatibilityStatus(dataDir)}");
    }

    private static void AppendRuntimeSlotSelectedFiles(StringBuilder sb, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected runtime game directory: {slot.GameDirectory}");
        sb.AppendLine($"Selected runtime PCK path: {slot.PckPath}");
        sb.AppendLine($"Selected runtime PCK SHA256: {slot.PckSha256}");
        sb.AppendLine($"Selected runtime release info path: {slot.ReleaseInfoPath}");
        sb.AppendLine($"Selected runtime release version: {slot.Metadata.ReleaseVersion}");
        sb.AppendLine($"Selected runtime release commit: {slot.Metadata.ReleaseCommit}");
        sb.AppendLine($"Selected runtime release build id: {slot.Metadata.ReleaseBuildId}");
        sb.AppendLine($"Selected runtime depot manifest count: {slot.Metadata.DepotManifestCount}");
        sb.AppendLine($"Selected runtime depots matching public: {slot.Metadata.DepotsMatchingPublic}");
        sb.AppendLine($"Selected runtime depots differing from public: {slot.Metadata.DepotsDifferingFromPublic}");
        sb.AppendLine($"Selected runtime depots inherited from public: {slot.Metadata.DepotsInheritedFromPublic}");
        sb.AppendLine($"Selected runtime depots missing selected manifest: {slot.Metadata.DepotsMissingSelectedManifest}");
        sb.AppendLine($"Selected runtime depot manifest fingerprint: {slot.Metadata.DepotManifestFingerprint}");
        sb.AppendLine($"Selected runtime identity summary: {slot.Metadata.IdentitySummary}");
        sb.AppendLine($"Selected runtime slot ID: {slot.RuntimeSlotId}");
        sb.AppendLine($"Selected runtime slot identity: {slot.RuntimeSlotIdentity.Replace("\n", " | ")}");
        sb.AppendLine($"Selected runtime source sts2.dll path: {slot.SourceAssemblyPath}");
        sb.AppendLine($"Selected runtime source sts2.dll exists: {BoolText(slot.SourceAssemblyExists)}");
        sb.AppendLine($"Selected runtime source sts2.dll SHA256: {slot.SourceAssemblySha256}");
        sb.AppendLine($"Selected runtime active Android sts2.dll path: {slot.ActiveAndroidAssemblyPath}");
        sb.AppendLine($"Selected runtime active Android sts2.dll exists: {BoolText(slot.ActiveAndroidAssemblyExists)}");
        sb.AppendLine($"Selected runtime active Android sts2.dll SHA256: {slot.ActiveAndroidAssemblySha256}");
        sb.AppendLine($"Selected runtime branch source available: {BoolText(slot.BranchRuntimeAvailable)}");
        sb.AppendLine($"Selected runtime source matches active Android assembly: {BoolText(slot.SourceMatchesActiveAndroidAssembly)}");
        sb.AppendLine($"Selected runtime uses legacy packaged public runtime: {BoolText(slot.UsesLegacyPackagedPublicRuntime)}");
    }
}
