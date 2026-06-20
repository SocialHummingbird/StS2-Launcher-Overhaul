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
        RuntimePackManifest runtimePack,
        PatchCompatibilityEvidence patchCompatibility,
        bool runtimePackSlotIdMatches,
        string runtimeSlotId,
        string runtimeSlotIdentity,
        string pckSha256,
        string sourceAssemblySha256,
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
        RuntimePack = runtimePack;
        PatchCompatibility = patchCompatibility;
        RuntimePackSlotIdMatches = runtimePackSlotIdMatches;
        RuntimeSlotId = runtimeSlotId;
        RuntimeSlotIdentity = runtimeSlotIdentity;
        PckSha256 = pckSha256;
        SourceAssemblySha256 = sourceAssemblySha256;
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
    internal RuntimePackManifest RuntimePack { get; }
    internal PatchCompatibilityEvidence PatchCompatibility { get; }
    internal bool RuntimePackSlotIdMatches { get; }
    internal string RuntimeSlotId { get; }
    internal string RuntimeSlotIdentity { get; }
    internal string PckSha256 { get; }
    internal string SourceAssemblySha256 { get; }
    internal string ActiveAndroidAssemblySha256 { get; }
    internal bool SourceAssemblyExists { get; }
    internal bool ActiveAndroidAssemblyExists { get; }
    internal bool RuntimePackManifestExists { get; }
}
