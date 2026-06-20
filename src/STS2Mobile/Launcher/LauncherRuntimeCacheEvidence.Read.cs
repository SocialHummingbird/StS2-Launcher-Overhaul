namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeCacheEvidence
{
    internal static string UtcMillis(string dataDir)
        => ReadMarkerValue(dataDir, UtcMillisPrefix);

    internal static string Package(string dataDir)
        => ReadMarkerValue(dataDir, PackagePrefix);

    internal static string VersionName(string dataDir)
        => ReadMarkerValue(dataDir, VersionNamePrefix);

    internal static string VersionCode(string dataDir)
        => ReadMarkerValue(dataDir, VersionCodePrefix);

    internal static string AssemblyCacheSchema(string dataDir)
        => ReadMarkerValue(dataDir, AssemblyCacheSchemaPrefix);

    internal static string SelectedBranch(string dataDir)
        => ReadMarkerValue(dataDir, SelectedBranchPrefix);

    internal static string SelectedBranchRequiresRuntimePack(string dataDir)
        => ReadMarkerValue(dataDir, SelectedBranchRequiresRuntimePackPrefix);

    internal static string RuntimeId(string dataDir)
        => ReadMarkerValue(dataDir, RuntimeIdPrefix);

    internal static string RuntimeSource(string dataDir)
        => ReadMarkerValue(dataDir, RuntimeSourcePrefix);

    internal static string RuntimePackDirectory(string dataDir)
        => ReadMarkerValue(dataDir, RuntimePackDirectoryPrefix);

    internal static string RuntimePackGameAssembly(string dataDir)
        => ReadMarkerValue(dataDir, RuntimePackGameAssemblyPrefix);

    internal static string GameDirectory(string dataDir)
        => ReadMarkerValue(dataDir, GameDirectoryPrefix);

    internal static string SelectedPckPath(string dataDir)
        => ReadMarkerValue(dataDir, SelectedPckPathPrefix);

    internal static string SelectedPckIdentity(string dataDir)
        => ReadMarkerValue(dataDir, SelectedPckIdentityPrefix);

    internal static string SelectedPckSha256(string dataDir)
        => ReadMarkerValue(dataDir, SelectedPckSha256Prefix);

    internal static string SelectedSourceAssembly(string dataDir)
        => ReadMarkerValue(dataDir, SelectedSourceAssemblyPrefix);

    internal static string SelectedSourceAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, SelectedSourceAssemblySha256Prefix);

    internal static string ActiveSourceAssembly(string dataDir)
        => ReadMarkerValue(dataDir, ActiveSourceAssemblyPrefix);

    internal static string ActiveSourceAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, ActiveSourceAssemblySha256Prefix);

    internal static string PublishCacheDirectory(string dataDir)
        => ReadMarkerValue(dataDir, PublishCacheDirectoryPrefix);

    internal static string PublishCacheActiveAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, PublishCacheActiveAssemblySha256Prefix);

    private static string ReadMarkerValue(string dataDir, string prefix)
        => LauncherMarkerFile.ReadValue(MarkerPath(dataDir), prefix);

    private static bool HasValue(string value)
        => LauncherMarkerFile.HasConcreteValue(value);
}
