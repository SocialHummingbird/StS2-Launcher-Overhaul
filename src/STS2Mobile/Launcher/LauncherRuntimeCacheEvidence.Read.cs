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

    internal static string ActiveBranch(string dataDir)
        => ReadMarkerValue(dataDir, ActiveBranchPrefix);

    internal static string GameIdentityId(string dataDir)
        => ReadMarkerValue(dataDir, GameIdentityIdPrefix);

    internal static string RuntimePackId(string dataDir)
        => ReadMarkerValue(dataDir, RuntimePackIdPrefix);

    internal static string RuntimePackDirectory(string dataDir)
        => ReadMarkerValue(dataDir, RuntimePackDirectoryPrefix);

    internal static string RuntimePackGameAssembly(string dataDir)
        => ReadMarkerValue(dataDir, RuntimePackGameAssemblyPrefix);

    internal static string RuntimePackAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, RuntimePackAssemblySha256Prefix);

    internal static string PublishCacheDirectory(string dataDir)
        => ReadMarkerValue(dataDir, PublishCacheDirectoryPrefix);

    internal static string PublishCacheActiveAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, PublishCacheActiveAssemblySha256Prefix);

    private static string ReadMarkerValue(string dataDir, string prefix)
        => LauncherMarkerFile.ReadValue(MarkerPath(dataDir), prefix);

}
