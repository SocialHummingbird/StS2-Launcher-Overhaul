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
        sb.AppendLine($"Runtime cache marker active branch: {LauncherRuntimeCacheEvidence.ActiveBranch(dataDir)}");
        sb.AppendLine($"Runtime cache marker game identity ID: {LauncherRuntimeCacheEvidence.GameIdentityId(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack ID: {LauncherRuntimeCacheEvidence.RuntimePackId(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack directory: {LauncherRuntimeCacheEvidence.RuntimePackDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack game assembly: {LauncherRuntimeCacheEvidence.RuntimePackGameAssembly(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack sts2.dll SHA256: {LauncherRuntimeCacheEvidence.RuntimePackAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker publish cache directory: {LauncherRuntimeCacheEvidence.PublishCacheDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker publish cache active sts2.dll SHA256: {LauncherRuntimeCacheEvidence.PublishCacheActiveAssemblySha256(dataDir)}");
    }
}
