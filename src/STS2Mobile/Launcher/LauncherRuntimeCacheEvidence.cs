using System.IO;

namespace STS2Mobile.Launcher;
internal static partial class LauncherRuntimeCacheEvidence
{
    internal const string MarkerFileName = "current_runtime_cache.txt";
    internal static string MarkerPath(string dataDir) => Path.Combine(dataDir, MarkerFileName);
    internal static bool MarkerPresent(string dataDir) => File.Exists(MarkerPath(dataDir));
    internal const string UtcMillisPrefix = "UTC millis:";
    internal const string PackagePrefix = "Package:";
    internal const string VersionNamePrefix = "Version name:";
    internal const string VersionCodePrefix = "Version code:";
    internal const string AssemblyCacheSchemaPrefix = "Assembly cache schema:";
    internal const string ActiveBranchPrefix = "Active branch:";
    internal const string GameIdentityIdPrefix = "Game identity ID:";
    internal const string RuntimePackIdPrefix = "Runtime pack ID:";
    internal const string RuntimePackDirectoryPrefix = "Runtime pack directory:";
    internal const string RuntimePackGameAssemblyPrefix = "Runtime pack game assembly:";
    internal const string RuntimePackAssemblySha256Prefix = "Runtime pack sts2.dll SHA256:";
    internal const string PublishCacheDirectoryPrefix = "Publish cache directory:";
    internal const string PublishCacheActiveAssemblySha256Prefix = "Publish cache active sts2.dll SHA256:";
    internal static Snapshot ReadSnapshot(string dataDir) => new(LauncherMarkerFile.ReadSnapshot(MarkerPath(dataDir)));
    internal sealed class Snapshot
    {
        private readonly LauncherMarkerFile.TextSnapshot _marker;
        internal Snapshot(LauncherMarkerFile.TextSnapshot marker) => _marker = marker;
        internal bool Present => _marker.Present;
        internal string UtcMillis => _marker.ReadValue(UtcMillisPrefix);
        internal string Package => _marker.ReadValue(PackagePrefix);
        internal string VersionName => _marker.ReadValue(VersionNamePrefix);
        internal string VersionCode => _marker.ReadValue(VersionCodePrefix);
        internal string AssemblyCacheSchema => _marker.ReadValue(AssemblyCacheSchemaPrefix);
        internal string ActiveBranch => _marker.ReadValue(ActiveBranchPrefix);
        internal string GameIdentityId => _marker.ReadValue(GameIdentityIdPrefix);
        internal string RuntimePackId => _marker.ReadValue(RuntimePackIdPrefix);
        internal string RuntimePackDirectory => _marker.ReadValue(RuntimePackDirectoryPrefix);
        internal string RuntimePackGameAssembly => _marker.ReadValue(RuntimePackGameAssemblyPrefix);
        internal string RuntimePackAssemblySha256 => _marker.ReadValue(RuntimePackAssemblySha256Prefix);
        internal string PublishCacheDirectory => _marker.ReadValue(PublishCacheDirectoryPrefix);
        internal string PublishCacheActiveAssemblySha256 => _marker.ReadValue(PublishCacheActiveAssemblySha256Prefix);
    }
}
