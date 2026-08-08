using System.Text;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendRuntimeCacheEvidence(StringBuilder sb, string dataDir, string branch)
    {
        sb.AppendLine($"Runtime cache marker filename: {LauncherRuntimeCacheEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime cache marker path: {LauncherRuntimeCacheEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime cache marker present: {BoolText(LauncherRuntimeCacheEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Runtime cache marker UTC millis: {LauncherRuntimeCacheEvidence.UtcMillis(dataDir)}");
        sb.AppendLine($"Runtime cache marker package: {LauncherRuntimeCacheEvidence.Package(dataDir)}");
        sb.AppendLine($"Runtime cache marker version name: {LauncherRuntimeCacheEvidence.VersionName(dataDir)}");
        sb.AppendLine($"Runtime cache marker version code: {LauncherRuntimeCacheEvidence.VersionCode(dataDir)}");
        sb.AppendLine($"Runtime cache marker assembly cache schema: {LauncherRuntimeCacheEvidence.AssemblyCacheSchema(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected branch: {LauncherRuntimeCacheEvidence.SelectedBranch(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected branch requires runtime pack: {LauncherRuntimeCacheEvidence.SelectedBranchRequiresRuntimePack(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime ID: {LauncherRuntimeCacheEvidence.RuntimeId(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime source: {LauncherRuntimeCacheEvidence.RuntimeSource(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack directory: {LauncherRuntimeCacheEvidence.RuntimePackDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack game assembly: {LauncherRuntimeCacheEvidence.RuntimePackGameAssembly(dataDir)}");
        sb.AppendLine($"Runtime cache marker game directory: {LauncherRuntimeCacheEvidence.GameDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected PCK path: {LauncherRuntimeCacheEvidence.SelectedPckPath(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected PCK identity: {LauncherRuntimeCacheEvidence.SelectedPckIdentity(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected PCK SHA256: {LauncherRuntimeCacheEvidence.SelectedPckSha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected source sts2.dll: {LauncherRuntimeCacheEvidence.SelectedSourceAssembly(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected source sts2.dll SHA256: {LauncherRuntimeCacheEvidence.SelectedSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker active source sts2.dll: {LauncherRuntimeCacheEvidence.ActiveSourceAssembly(dataDir)}");
        sb.AppendLine($"Runtime cache marker active source sts2.dll SHA256: {LauncherRuntimeCacheEvidence.ActiveSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker publish cache directory: {LauncherRuntimeCacheEvidence.PublishCacheDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker publish cache active sts2.dll SHA256: {LauncherRuntimeCacheEvidence.PublishCacheActiveAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker matches selected branch: {BoolText(LauncherRuntimeCacheEvidence.MatchesSelectedBranch(dataDir, branch))}");
        sb.AppendLine($"Runtime cache marker selected PCK matches selected runtime: {BoolText(LauncherRuntimeCacheEvidence.PckMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime cache marker selected source sts2.dll matches selected runtime: {BoolText(LauncherRuntimeCacheEvidence.SourceAssemblyMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime cache publish sts2.dll matches selected runtime: {BoolText(LauncherRuntimeCacheEvidence.PublishCacheMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime cache prepared for selected runtime: {BoolText(LauncherRuntimeCacheEvidence.CachePreparedForSelectedRuntime(dataDir, branch))}");
    }
}
