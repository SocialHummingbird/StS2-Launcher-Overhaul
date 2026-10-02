using System.Text;

namespace STS2Mobile.Launcher;
internal static partial class LauncherDiagnostics
{
    private static void AppendGameRuntimeSlot(StringBuilder sb, string dataDir, string branch, LauncherLaunchReadiness launchReadiness)
    {
        var slot = launchReadiness?.RuntimeSlot ?? GameRuntimeSlot.Inspect(dataDir, branch);
        AppendRuntimeSlotSummary(sb, dataDir, branch, slot);
        AppendRuntimeSlotSelectedFiles(sb, slot);
        AppendRuntimePackEvidence(sb, slot);
        AppendPatchCompatibilityEvidence(sb, slot);
        AppendRuntimePatchValidationEvidence(sb, dataDir);
        AppendRuntimeCacheEvidence(sb, dataDir, branch);
        AppendRuntimePairingSummary(sb, slot);
    }

    private static void AppendRuntimePairingSummary(StringBuilder sb, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected runtime pairing status: {slot.RuntimePairingStatus}");
        sb.AppendLine($"Selected runtime requires usable runtime pack: {BoolText(slot.RequiresRuntimePackOrPreparedCache)}");
        sb.AppendLine($"Selected runtime compatible: {BoolText(slot.RuntimeCompatible)}");
        sb.AppendLine($"Selected runtime playable: {BoolText(slot.Playable)}");
    }

    private static void AppendPatchCompatibilityEvidence(StringBuilder sb, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected patch compatibility source: {slot.PatchCompatibility.Source}");
        sb.AppendLine($"Selected patch compatibility marker path: {ValueOrMissing(slot.PatchCompatibility.MarkerPath)}");
        sb.AppendLine($"Selected patch compatibility evidence present: {BoolText(slot.PatchCompatibility.Exists)}");
        sb.AppendLine($"Selected patch compatibility evidence readable: {BoolText(slot.PatchCompatibility.Readable)}");
        sb.AppendLine($"Selected patch compatibility status: {slot.PatchCompatibility.Status}");
        sb.AppendLine($"Selected patch compatibility detail: {ValueOrMissing(slot.PatchCompatibility.Detail)}");
        sb.AppendLine($"Selected patch compatibility expected game identity: {ValueOrMissing(slot.PatchCompatibility.ExpectedGameIdentity?.Id)}");
        sb.AppendLine($"Selected patch compatibility validated game identity: {ValueOrMissing(slot.PatchCompatibility.ValidatedGameIdentity?.Id)}");
        sb.AppendLine($"Selected patch compatibility game identity matches: {BoolText(slot.PatchCompatibility.GameIdentityMatches)}");
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
        var evidence = LauncherRuntimePatchValidationEvidence.ReadSnapshot(dataDir);
        sb.AppendLine($"Runtime patch validation marker filename: {LauncherRuntimePatchValidationEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime patch validation marker path: {LauncherRuntimePatchValidationEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime patch validation marker present: {BoolText(evidence.Present)}");
        sb.AppendLine($"Runtime patch validation UTC: {evidence.Utc}");
        sb.AppendLine($"Runtime patch validation UTC parseable: {BoolText(evidence.UtcParseable)}");
        sb.AppendLine($"Runtime patch validation status: {evidence.Status}");
        sb.AppendLine($"Runtime patch validation selected branch: {evidence.SelectedBranch}");
        sb.AppendLine($"Runtime patch validation selected version: {evidence.SelectedVersion}");
        sb.AppendLine($"Runtime patch validation game identity ID: {evidence.GameIdentityId}");
        sb.AppendLine($"Runtime patch validation selected PCK SHA256: {evidence.SelectedPckSha256}");
        sb.AppendLine($"Runtime patch validation selected source sts2.dll SHA256: {evidence.SelectedSourceAssemblySha256}");
        sb.AppendLine($"Runtime patch validation active Android sts2.dll SHA256: {evidence.ActiveAndroidAssemblySha256}");
        sb.AppendLine($"Runtime patch validation runtime pack id: {ValueOrMissing(evidence.RuntimePackId)}");
        sb.AppendLine($"Runtime patch validation runtime pack status: {evidence.RuntimePackStatus}");
        sb.AppendLine($"Runtime patch validation applied patch count: {evidence.AppliedPatchCount}");
        sb.AppendLine($"Runtime patch validation failed patch count: {evidence.FailedPatchCount}");
        sb.AppendLine($"Runtime patch validation total patch count: {evidence.TotalPatchCount}");
        sb.AppendLine($"Runtime patch validation failure messages: {evidence.FailureMessages}");
    }

    private static void AppendRuntimeCacheEvidence(StringBuilder sb, string dataDir, string branch)
    {
        var evidence = LauncherRuntimeCacheEvidence.ReadSnapshot(dataDir);
        sb.AppendLine($"Runtime cache marker filename: {LauncherRuntimeCacheEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime cache marker path: {LauncherRuntimeCacheEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime cache marker present: {BoolText(evidence.Present)}");
        sb.AppendLine($"Runtime cache marker UTC millis: {evidence.UtcMillis}");
        sb.AppendLine($"Runtime cache marker package: {evidence.Package}");
        sb.AppendLine($"Runtime cache marker version name: {evidence.VersionName}");
        sb.AppendLine($"Runtime cache marker version code: {evidence.VersionCode}");
        sb.AppendLine($"Runtime cache marker assembly cache schema: {evidence.AssemblyCacheSchema}");
        sb.AppendLine($"Runtime cache marker active branch: {evidence.ActiveBranch}");
        sb.AppendLine($"Runtime cache marker game identity ID: {evidence.GameIdentityId}");
        sb.AppendLine($"Runtime cache marker runtime pack ID: {evidence.RuntimePackId}");
        sb.AppendLine($"Runtime cache marker runtime pack directory: {evidence.RuntimePackDirectory}");
        sb.AppendLine($"Runtime cache marker runtime pack game assembly: {evidence.RuntimePackGameAssembly}");
        sb.AppendLine($"Runtime cache marker runtime pack sts2.dll SHA256: {evidence.RuntimePackAssemblySha256}");
        sb.AppendLine($"Runtime cache marker publish cache directory: {evidence.PublishCacheDirectory}");
        sb.AppendLine($"Runtime cache marker publish cache active sts2.dll SHA256: {evidence.PublishCacheActiveAssemblySha256}");
    }

    private static void AppendRuntimePackEvidence(StringBuilder sb, GameRuntimeSlot slot)
    {
        sb.AppendLine($"Selected runtime pack manifest path: {slot.RuntimePackManifestPath}");
        sb.AppendLine($"Selected runtime pack manifest present: {BoolText(slot.RuntimePackManifestExists)}");
        sb.AppendLine($"Selected runtime pack status: {slot.RuntimePack.Status}");
        sb.AppendLine($"Selected runtime pack usability status: {slot.RuntimePackUsabilityStatus}");
        sb.AppendLine($"Selected runtime pack usable: {BoolText(slot.RuntimePackUsable)}");
        sb.AppendLine($"Selected runtime pack id: {ValueOrMissing(slot.RuntimePack.PackId)}");
        sb.AppendLine($"Selected runtime pack game identity ID: {ValueOrMissing(slot.RuntimePack.GameIdentityId)}");
        sb.AppendLine($"Selected runtime pack install generation: {ValueOrMissing(slot.RuntimePack.InstallGeneration)}");
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

    private static void AppendRuntimeSlotSummary(StringBuilder sb, string dataDir, string branch, GameRuntimeSlot slot)
    {
        var evidence = LauncherRuntimeSlotEvidence.ReadSnapshot(dataDir);
        sb.AppendLine($"Selected runtime slot branch: {slot.Branch}");
        sb.AppendLine($"Selected runtime slot display name: {slot.DisplayName}");
        sb.AppendLine($"Selected runtime slot kind: {slot.SlotKind}");
        sb.AppendLine($"Selected runtime slot directory: {slot.SlotDirectory}");
        sb.AppendLine($"Runtime slot evidence marker filename: {LauncherRuntimeSlotEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime slot evidence marker path: {LauncherRuntimeSlotEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime slot evidence marker present: {BoolText(evidence.Present)}");
        sb.AppendLine($"Runtime slot evidence selected branch: {evidence.Branch}");
        sb.AppendLine($"Runtime slot evidence game identity ID: {evidence.GameIdentityId}");
        sb.AppendLine($"Runtime slot evidence files ready: {evidence.FilesReady}");
        sb.AppendLine($"Runtime slot evidence readiness problem: {ValueOrMissing(evidence.ReadinessProblem)}");
        sb.AppendLine($"Runtime slot evidence runtime pack usability status: {evidence.RuntimePackUsabilityStatus}");
        sb.AppendLine($"Runtime slot evidence patch compatibility status: {evidence.PatchCompatibilityStatus}");
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
        sb.AppendLine($"Selected game identity ID: {slot.GameIdentityId}");
        sb.AppendLine($"Selected game identity problem: {ValueOrMissing(slot.GameIdentityProblem)}");
        sb.AppendLine($"Selected runtime source sts2.dll path: {slot.SourceAssemblyPath}");
        sb.AppendLine($"Selected runtime source sts2.dll exists: {BoolText(slot.SourceAssemblyExists)}");
        sb.AppendLine($"Selected runtime source sts2.dll SHA256: {slot.SourceAssemblySha256}");
        sb.AppendLine($"Selected runtime active Android sts2.dll path: {slot.ActiveAndroidAssemblyPath}");
        sb.AppendLine($"Selected runtime active Android sts2.dll exists: {BoolText(slot.ActiveAndroidAssemblyExists)}");
        sb.AppendLine($"Selected runtime active Android sts2.dll SHA256: {slot.ActiveAndroidAssemblySha256}");
        sb.AppendLine($"Selected runtime prepared Android sts2.dll SHA256: {slot.PreparedAndroidAssemblySha256}");
        sb.AppendLine($"Selected runtime active Android assembly matches prepared runtime: {BoolText(slot.ActiveAndroidAssemblyMatchesPreparedRuntime)}");
        sb.AppendLine($"Selected runtime requires process restart for prepared runtime: {BoolText(slot.RequiresProcessRestartForPreparedRuntime)}");
        sb.AppendLine($"Selected runtime branch source available: {BoolText(slot.BranchRuntimeAvailable)}");
        sb.AppendLine($"Selected runtime source matches active Android assembly: {BoolText(slot.SourceMatchesActiveAndroidAssembly)}");
    }
}
