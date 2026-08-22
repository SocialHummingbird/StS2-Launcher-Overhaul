namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private const string GameAssemblyFileName = "sts2.dll";
    private const string RuntimePacksDirectory = "runtime_packs";
    private const string CompatibilityManifestFileName = "compatibility.json";

    private GameRuntimeSlot(
        string branch,
        string displayName,
        string slotKind,
        string slotDirectory,
        string gameDirectory,
        string pckPath,
        string releaseInfoPath,
        string sourceAssemblyPath,
        string activeAndroidAssemblyPath,
        string runtimePackManifestPath,
        RuntimeSlotMetadata metadata,
        GameIdentity gameIdentity,
        string gameIdentityProblem,
        RuntimePackManifest runtimePack,
        PatchCompatibilityEvidence patchCompatibility,
        string activeAndroidAssemblySha256,
        bool sourceAssemblyExists,
        bool activeAndroidAssemblyExists,
        bool runtimePackManifestExists
    )
    {
        Branch = branch;
        DisplayName = displayName;
        SlotKind = slotKind;
        SlotDirectory = slotDirectory;
        GameDirectory = gameDirectory;
        PckPath = pckPath;
        ReleaseInfoPath = releaseInfoPath;
        SourceAssemblyPath = sourceAssemblyPath;
        ActiveAndroidAssemblyPath = activeAndroidAssemblyPath;
        RuntimePackManifestPath = runtimePackManifestPath;
        Metadata = metadata;
        GameIdentity = gameIdentity;
        GameIdentityProblem = gameIdentityProblem ?? string.Empty;
        RuntimePack = runtimePack;
        PatchCompatibility = patchCompatibility;
        ActiveAndroidAssemblySha256 = activeAndroidAssemblySha256;
        SourceAssemblyExists = sourceAssemblyExists;
        ActiveAndroidAssemblyExists = activeAndroidAssemblyExists;
        RuntimePackManifestExists = runtimePackManifestExists;
    }

    internal string Branch { get; }
    internal string DisplayName { get; }
    internal string SlotKind { get; }
    internal string SlotDirectory { get; }
    internal string GameDirectory { get; }
    internal string PckPath { get; }
    internal string ReleaseInfoPath { get; }
    internal string SourceAssemblyPath { get; }
    internal string ActiveAndroidAssemblyPath { get; }
    internal string RuntimePackManifestPath { get; }
    internal RuntimeSlotMetadata Metadata { get; }
    internal GameIdentity GameIdentity { get; }
    internal string GameIdentityProblem { get; }
    internal RuntimePackManifest RuntimePack { get; }
    internal PatchCompatibilityEvidence PatchCompatibility { get; }
    internal string GameIdentityId => GameIdentity?.Id ?? "<missing>";
    internal string InstallGeneration => GameIdentity?.InstallGeneration ?? "<missing>";
    internal string PckSha256 => GameIdentity?.PckSha256 ?? "<missing>";
    internal string SourceAssemblySha256 => GameIdentity?.SourceAssemblySha256 ?? "<missing>";
    internal string ActiveAndroidAssemblySha256 { get; }
    internal bool SourceAssemblyExists { get; }
    internal bool ActiveAndroidAssemblyExists { get; }
    internal bool RuntimePackManifestExists { get; }
}
