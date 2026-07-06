using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchReadiness
{
    private LauncherLaunchReadiness(
        string dataDir,
        string branch,
        bool ready,
        string readinessProblem,
        GameRuntimeSlot runtimeSlot,
        string evaluationPhase,
        string cacheStatus
    )
    {
        DataDir = dataDir ?? string.Empty;
        Branch = SteamGameBranch.Normalize(branch);
        Ready = ready;
        ReadinessProblem = string.IsNullOrWhiteSpace(readinessProblem)
            ? string.Empty
            : readinessProblem;
        RuntimeSlot = runtimeSlot;
        EvaluationPhase = string.IsNullOrWhiteSpace(evaluationPhase)
            ? string.Empty
            : evaluationPhase;
        CacheStatus = string.IsNullOrWhiteSpace(cacheStatus)
            ? LauncherLaunchReadinessCacheStatus.Fresh
            : cacheStatus;
    }

    private string DataDir { get; }
    internal string Branch { get; }
    internal bool Ready { get; }
    internal string ReadinessProblem { get; }
    internal GameRuntimeSlot RuntimeSlot { get; }
    internal string EvaluationPhase { get; }
    internal string CacheStatus { get; }
    internal bool HasRuntimeSlot => RuntimeSlot != null;

    internal string RuntimeSlotId => HasRuntimeSlot ? RuntimeSlot.RuntimeSlotId : "<none>";
    internal string RuntimePairingStatus => HasRuntimeSlot ? RuntimeSlot.RuntimePairingStatus : "<not inspected>";
    internal string PatchCompatibilityStatus => HasRuntimeSlot ? RuntimeSlot.PatchCompatibility?.Status ?? "<none>" : "<not inspected>";
    internal string GameDirectory => HasRuntimeSlot ? RuntimeSlot.GameDirectory : "<none>";
    internal string PckPath => HasRuntimeSlot ? RuntimeSlot.PckPath : "<none>";
    internal string SourceAssemblyPath => HasRuntimeSlot ? RuntimeSlot.SourceAssemblyPath : "<none>";
    internal string ActiveAndroidAssemblyPath => HasRuntimeSlot ? RuntimeSlot.ActiveAndroidAssemblyPath : "<none>";
    internal string RuntimePackDirectory => HasRuntimeSlot ? Path.GetDirectoryName(RuntimeSlot.RuntimePackManifestPath) ?? "<none>" : "<none>";
    internal string RuntimePackManifestPath => HasRuntimeSlot ? RuntimeSlot.RuntimePackManifestPath : "<none>";
    internal string RuntimeCacheMarkerPath => HasDataDir ? LauncherRuntimeCacheEvidence.MarkerPath(DataDir) : "<none>";
    internal bool RuntimeCacheMarkerPresent => HasDataDir && LauncherRuntimeCacheEvidence.MarkerPresent(DataDir);
    internal string RuntimePatchValidationMarkerPath => HasDataDir ? LauncherRuntimePatchValidationEvidence.MarkerPath(DataDir) : "<none>";
    internal bool RuntimePatchValidationMarkerPresent => HasDataDir && LauncherRuntimePatchValidationEvidence.MarkerPresent(DataDir);
    internal string PatchCompatibilityMarkerPath => HasRuntimeSlot ? RuntimeSlot.PatchCompatibility?.MarkerPath ?? "<none>" : "<not inspected>";
    internal string PckSha256 => HasRuntimeSlot ? RuntimeSlot.PckSha256 : "<none>";
    internal string SourceAssemblySha256 => HasRuntimeSlot ? RuntimeSlot.SourceAssemblySha256 : "<none>";
    internal string ActiveAndroidAssemblySha256 => HasRuntimeSlot ? RuntimeSlot.ActiveAndroidAssemblySha256 : "<none>";
    internal string RuntimePackStatus => HasRuntimeSlot ? RuntimeSlot.RuntimePack?.Status ?? "<none>" : "<not inspected>";
    internal bool RuntimePackUsable => HasRuntimeSlot && RuntimeSlot.RuntimePackUsable;
    private bool HasDataDir => !string.IsNullOrWhiteSpace(DataDir);

    internal LauncherLaunchReadiness WithCacheStatus(string phase, string cacheStatus)
        => new(
            DataDir,
            Branch,
            Ready,
            ReadinessProblem,
            RuntimeSlot,
            phase,
            cacheStatus
        );
}
