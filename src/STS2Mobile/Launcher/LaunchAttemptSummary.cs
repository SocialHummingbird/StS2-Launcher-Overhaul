using System;

namespace STS2Mobile.Launcher;

internal readonly struct LaunchAttemptSummary
{
    internal LaunchAttemptSummary(
        string utc,
        string attemptId,
        string phase,
        string action,
        string source,
        string detail,
        string preparedReadinessUsed,
        string launchAttemptElapsedMs,
        string launchReadinessElapsedMs,
        string modReadinessElapsedMs,
        string readinessEvaluationPhase,
        string readinessCacheStatus,
        string selectedBranch,
        string selectedVersion,
        string filesReady,
        string readinessProblem,
        string runtimeSlotInspected,
        string runtimeSlotId,
        string runtimePairingStatus,
        string patchCompatibilityStatus,
        string gameDirectory,
        string pckPath,
        string pckSha256,
        string sourceAssemblyPath,
        string sourceAssemblySha256,
        string activeAndroidAssemblyPath,
        string activeAndroidAssemblySha256,
        string runtimePackUsable,
        string runtimePackDirectory,
        string runtimePackManifestPath,
        string runtimePackStatus,
        string runtimeCacheMarkerPath,
        string runtimeCacheMarkerPresent,
        string runtimePatchValidationMarkerPath,
        string runtimePatchValidationMarkerPresent,
        string patchCompatibilityMarkerPath,
        string modReadinessPhase,
        string modReadinessCacheStatus,
        string modPlayMode,
        string modInstalledCount,
        string modEnabledCount,
        string modUnsupportedCount,
        string selectedMods
    )
    {
        Present = true;
        Utc = Clean(utc);
        AttemptId = Clean(attemptId);
        Phase = Clean(phase);
        Action = Clean(action);
        Source = Clean(source);
        Detail = Clean(detail);
        PreparedReadinessUsed = Clean(preparedReadinessUsed);
        LaunchAttemptElapsedMs = Clean(launchAttemptElapsedMs);
        LaunchReadinessElapsedMs = Clean(launchReadinessElapsedMs);
        ModReadinessElapsedMs = Clean(modReadinessElapsedMs);
        ReadinessEvaluationPhase = Clean(readinessEvaluationPhase);
        ReadinessCacheStatus = Clean(readinessCacheStatus);
        SelectedBranch = Clean(selectedBranch);
        SelectedVersion = Clean(selectedVersion);
        FilesReady = Clean(filesReady);
        ReadinessProblem = Clean(readinessProblem);
        RuntimeSlotInspected = Clean(runtimeSlotInspected);
        RuntimeSlotId = Clean(runtimeSlotId);
        RuntimePairingStatus = Clean(runtimePairingStatus);
        PatchCompatibilityStatus = Clean(patchCompatibilityStatus);
        GameDirectory = Clean(gameDirectory);
        PckPath = Clean(pckPath);
        PckSha256 = Clean(pckSha256);
        SourceAssemblyPath = Clean(sourceAssemblyPath);
        SourceAssemblySha256 = Clean(sourceAssemblySha256);
        ActiveAndroidAssemblyPath = Clean(activeAndroidAssemblyPath);
        ActiveAndroidAssemblySha256 = Clean(activeAndroidAssemblySha256);
        RuntimePackUsable = Clean(runtimePackUsable);
        RuntimePackDirectory = Clean(runtimePackDirectory);
        RuntimePackManifestPath = Clean(runtimePackManifestPath);
        RuntimePackStatus = Clean(runtimePackStatus);
        RuntimeCacheMarkerPath = Clean(runtimeCacheMarkerPath);
        RuntimeCacheMarkerPresent = Clean(runtimeCacheMarkerPresent);
        RuntimePatchValidationMarkerPath = Clean(runtimePatchValidationMarkerPath);
        RuntimePatchValidationMarkerPresent = Clean(runtimePatchValidationMarkerPresent);
        PatchCompatibilityMarkerPath = Clean(patchCompatibilityMarkerPath);
        ModReadinessPhase = Clean(modReadinessPhase);
        ModReadinessCacheStatus = Clean(modReadinessCacheStatus);
        ModPlayMode = Clean(modPlayMode);
        ModInstalledCount = Clean(modInstalledCount);
        ModEnabledCount = Clean(modEnabledCount);
        ModUnsupportedCount = Clean(modUnsupportedCount);
        SelectedMods = Clean(selectedMods);
    }

    internal bool Present { get; }
    internal string Utc { get; }
    internal string AttemptId { get; }
    internal string Phase { get; }
    internal string Action { get; }
    internal string Source { get; }
    internal string Detail { get; }
    internal string PreparedReadinessUsed { get; }
    internal string LaunchAttemptElapsedMs { get; }
    internal string LaunchReadinessElapsedMs { get; }
    internal string ModReadinessElapsedMs { get; }
    internal string ReadinessEvaluationPhase { get; }
    internal string ReadinessCacheStatus { get; }
    internal string SelectedBranch { get; }
    internal string SelectedVersion { get; }
    internal string FilesReady { get; }
    internal string ReadinessProblem { get; }
    internal string RuntimeSlotInspected { get; }
    internal string RuntimeSlotId { get; }
    internal string RuntimePairingStatus { get; }
    internal string PatchCompatibilityStatus { get; }
    internal string GameDirectory { get; }
    internal string PckPath { get; }
    internal string PckSha256 { get; }
    internal string SourceAssemblyPath { get; }
    internal string SourceAssemblySha256 { get; }
    internal string ActiveAndroidAssemblyPath { get; }
    internal string ActiveAndroidAssemblySha256 { get; }
    internal string RuntimePackUsable { get; }
    internal string RuntimePackDirectory { get; }
    internal string RuntimePackManifestPath { get; }
    internal string RuntimePackStatus { get; }
    internal string RuntimeCacheMarkerPath { get; }
    internal string RuntimeCacheMarkerPresent { get; }
    internal string RuntimePatchValidationMarkerPath { get; }
    internal string RuntimePatchValidationMarkerPresent { get; }
    internal string PatchCompatibilityMarkerPath { get; }
    internal string ModReadinessPhase { get; }
    internal string ModReadinessCacheStatus { get; }
    internal string ModPlayMode { get; }
    internal string ModInstalledCount { get; }
    internal string ModEnabledCount { get; }
    internal string ModUnsupportedCount { get; }
    internal string SelectedMods { get; }

    internal static LaunchAttemptSummary Missing()
        => default;

    internal string ProblemOrDetail()
        => HasConcrete(ReadinessProblem)
            ? ReadinessProblem
            : HasConcrete(Detail)
                ? Detail
                : string.Empty;

    internal string ShortLine()
    {
        if (!Present)
            return string.Empty;

        var version = HasConcrete(SelectedVersion)
            ? SelectedVersion
            : SelectedBranch;
        var when = HasConcrete(Utc)
            ? $" at {Utc}"
            : string.Empty;
        var source = HasConcrete(Source)
            ? $" from {Source}"
            : string.Empty;
        var attempt = HasConcrete(AttemptId)
            ? $" [{AttemptId}]"
            : string.Empty;
        return $"Last Start Game attempt{attempt}: {Value(Action)}{source} reached {Value(Phase)}{when} for {Value(version)}; files ready: {Value(FilesReady)}; readiness: {Value(ReadinessCacheStatus)}.";
    }

    internal string RuntimeLine()
    {
        if (!Present)
            return string.Empty;

        return $"Runtime evidence: slot {Value(RuntimeSlotId)}, pairing {Value(RuntimePairingStatus)}, patch {Value(PatchCompatibilityStatus)}, runtime pack usable: {Value(RuntimePackUsable)}.";
    }

    internal string PathLine()
    {
        if (!Present)
            return string.Empty;

        return $"Launch paths: game {Value(GameDirectory)}, PCK {Value(PckPath)}, source sts2.dll {Value(SourceAssemblyPath)}, active Android sts2.dll {Value(ActiveAndroidAssemblyPath)}, runtime pack {Value(RuntimePackDirectory)}.";
    }

    internal string IdentityLine()
    {
        if (!Present || (!HasConcrete(PckSha256) && !HasConcrete(SourceAssemblySha256) && !HasConcrete(ActiveAndroidAssemblySha256)))
            return string.Empty;

        return $"Runtime identity: PCK {ShortHash(PckSha256)}, source sts2.dll {ShortHash(SourceAssemblySha256)}, active Android sts2.dll {ShortHash(ActiveAndroidAssemblySha256)}.";
    }

    internal string MarkerLine()
    {
        if (!Present)
            return string.Empty;

        return $"Launch markers: runtime pack manifest {Value(RuntimePackManifestPath)}, runtime cache {Value(RuntimeCacheMarkerPresent)} at {Value(RuntimeCacheMarkerPath)}, runtime patch validation {Value(RuntimePatchValidationMarkerPresent)} at {Value(RuntimePatchValidationMarkerPath)}, patch compatibility at {Value(PatchCompatibilityMarkerPath)}.";
    }

    internal string ModLine()
    {
        if (!Present || !HasConcrete(ModPlayMode))
            return string.Empty;

        var selected = HasConcrete(SelectedMods)
            ? SelectedMods
            : "<none>";
        return $"Mod evidence: mode {Value(ModPlayMode)}, readiness phase {Value(ModReadinessPhase)}, installed {Value(ModInstalledCount)}, enabled {Value(ModEnabledCount)}, unsupported {Value(ModUnsupportedCount)}, readiness {Value(ModReadinessCacheStatus)}, selected: {selected}.";
    }

    internal string TimingLine()
    {
        if (!Present || !HasConcrete(LaunchAttemptElapsedMs))
            return string.Empty;

        return $"Launch timing: total {Value(LaunchAttemptElapsedMs)}ms, readiness {Value(LaunchReadinessElapsedMs)}ms, mods {Value(ModReadinessElapsedMs)}ms.";
    }

    internal string RecoveryHint()
    {
        if (!Present)
            return string.Empty;

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.Checking))
            return "Suggested next step: Start Game stopped while checking the selected version. Try Start Game again, then create a support report if it repeats.";

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.SetupFailed))
            return HasConcrete(ProblemOrDetail())
                ? $"Suggested next step: {ProblemOrDetail()}"
                : "Suggested next step: reopen the launcher and create a support report if launch setup fails again.";

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.ReadinessFailed))
            return "Suggested next step: redownload the selected version to rebuild runtime evidence, then try Start Game again.";

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.ModReadinessFailed))
            return "Suggested next step: switch to Play Vanilla once. If vanilla starts, re-enable mods one at a time and create a support report for the failing mod set.";

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.RestartRequestedWithoutReadyFiles))
            return "Suggested next step: redownload the selected version. Android restart was requested even though the final bridge check did not have ready files.";

        if (EqualsText(FilesReady, "false") || ContainsText(Phase, "blocked"))
            return HasConcrete(ProblemOrDetail())
                ? $"Suggested next step: {ProblemOrDetail()}"
                : "Suggested next step: redownload the selected version, then try Start Game again.";

        if (EqualsText(RuntimePackUsable, "false") || ContainsText(RuntimePairingStatus, "not usable"))
            return "Suggested next step: redownload the selected version to rebuild runtime-pack and patch-validation evidence.";

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.LaunchHandoffNotRequested))
            return "Suggested next step: create a support report before retrying. The launcher finished readiness but did not request Android to start the game.";

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.LaunchHandoffFailed)
            || ContainsText(Phase, LauncherLaunchAttemptPhases.InProcessSignalFailed))
            return "Suggested next step: try Safe Start once. If it repeats, create a support report because Android handoff failed after readiness passed.";

        if (ContainsText(Phase, LauncherLaunchAttemptPhases.SafeAndroidRestartRequested))
            return "Suggested next step: Safe Start was already requested. Create a support report if the game still failed after this restart.";

        if (ContainsText(Phase, "restart") || ContainsText(Phase, "in-process"))
            return "Suggested next step: try Safe Start once, then create a support report if startup still stalls.";

        return "Suggested next step: open Help, try Safe Start once, then view the last error or create a support report if startup still fails.";
    }

    private static string Clean(string value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();

    private static string Value(string value)
        => HasConcrete(value) ? value : "<unknown>";

    private static string ShortHash(string value)
        => HasConcrete(value)
            ? value.Length > 12 ? value.Substring(0, 12) : value
            : "<unknown>";

    private static bool HasConcrete(string value)
        => !string.IsNullOrWhiteSpace(value)
            && !value.StartsWith("<", StringComparison.OrdinalIgnoreCase);

    private static bool EqualsText(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsText(string value, string text)
        => !string.IsNullOrWhiteSpace(value)
            && value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
}
