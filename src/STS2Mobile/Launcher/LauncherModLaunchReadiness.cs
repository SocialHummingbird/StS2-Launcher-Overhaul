using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace STS2Mobile.Launcher;

internal sealed class LauncherModLaunchReadiness
{
    private LauncherModLaunchReadiness(
        string playMode,
        int installedMods,
        int enabledMods,
        int unsupportedMods,
        string selectedMods,
        string modSetFingerprint,
        string phase,
        string cacheStatus,
        LauncherModLaunchPlan plan
    )
    {
        PlayMode = string.IsNullOrWhiteSpace(playMode)
            ? LauncherModSelectionState.VanillaModeName
            : playMode;
        InstalledMods = installedMods;
        EnabledMods = enabledMods;
        UnsupportedMods = unsupportedMods;
        SelectedMods = string.IsNullOrWhiteSpace(selectedMods)
            ? "<none>"
            : selectedMods;
        ModSetFingerprint = string.IsNullOrWhiteSpace(modSetFingerprint)
            ? string.Empty
            : modSetFingerprint.Trim().ToLowerInvariant();
        Phase = string.IsNullOrWhiteSpace(phase) ? string.Empty : phase;
        CacheStatus = string.IsNullOrWhiteSpace(cacheStatus)
            ? LauncherModLaunchReadinessCacheStatus.Fresh
            : cacheStatus;
        Plan = plan ?? LauncherModLaunchPlan.VanillaPlan();
    }

    internal string PlayMode { get; }
    internal int InstalledMods { get; }
    internal int EnabledMods { get; }
    internal int UnsupportedMods { get; }
    internal string SelectedMods { get; }
    internal string ModSetFingerprint { get; }
    internal string Phase { get; }
    internal string CacheStatus { get; }
    internal LauncherModLaunchPlan Plan { get; }

    internal bool IsModded
        => string.Equals(PlayMode, LauncherModSelectionState.ModdedModeName, StringComparison.OrdinalIgnoreCase);

    internal string Summary
        => IsModded
            ? $"modded; enabled={EnabledMods}; installed={InstalledMods}; unsupported={UnsupportedMods}; selected={SelectedMods}"
            : "vanilla; runtime mod scan skipped";

    internal static LauncherModLaunchReadiness Evaluate(string phase)
    {
        var document = LauncherModSelectionState.Load();
        return Evaluate(phase, document);
    }

    internal static LauncherModLaunchReadiness Evaluate(string phase, LauncherModSelectionDocument document)
    {
        if (!LauncherModSelectionState.IsModdedModeFor(document))
        {
            LauncherModLaunchResultStore.WriteVanilla(document);
            return Vanilla(phase);
        }

        var identity = LauncherModSourceIdentity.Create();
        if (LauncherModLaunchReadinessCache.TryGet(identity, phase, out var cached))
        {
            LauncherModLaunchResultStore.WritePlannedNotStarted(document, cached.Plan);
            return cached;
        }

        var snapshot = LauncherModSelectionState.KnownModsSnapshot(document, identity);
        var resolution = LauncherModLaunchPlan.Resolve(document, snapshot.Mods);
        if (!resolution.Success)
        {
            LauncherModLaunchResultStore.WritePlanFailure(document, resolution.Error);
            throw new InvalidDataException(
                $"Mod discovery {resolution.Error.Code}: {resolution.Error.Message}"
            );
        }

        LauncherModLaunchResultStore.WritePlannedNotStarted(document, resolution.Plan);
        var readiness = EvaluateFresh(phase, snapshot.Mods, resolution.Plan);
        LauncherModLaunchReadinessCache.Store(snapshot.Identity, readiness);
        return readiness;
    }

    internal static LauncherModLaunchReadiness Vanilla(string phase)
        => EvaluateVanilla(phase);

    internal LauncherModLaunchReadiness WithCacheStatus(string phase, string cacheStatus)
        => new(
            PlayMode,
            InstalledMods,
            EnabledMods,
            UnsupportedMods,
            SelectedMods,
            ModSetFingerprint,
            phase,
            cacheStatus,
            Plan
        );

    private static LauncherModLaunchReadiness EvaluateFresh(
        string phase,
        IReadOnlyList<LauncherKnownMod> knownMods,
        LauncherModLaunchPlan plan
    )
    {
        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: checking mod selector",
            $"mode={LauncherModSelectionState.ModdedModeName}"
        );

        knownMods ??= Array.Empty<LauncherKnownMod>();
        var selectedMods = plan.EnabledMods
            .Select(mod => string.IsNullOrWhiteSpace(mod.Name) ? mod.ManifestId : mod.Name)
            .Take(16)
            .ToArray();
        var selectedModSummary = string.Join(", ", selectedMods);
        if (plan.EnabledMods.Length > selectedMods.Length)
            selectedModSummary = $"{selectedModSummary}, +{plan.EnabledMods.Length - selectedMods.Length} more";

        return new LauncherModLaunchReadiness(
            LauncherModSelectionState.ModdedModeName,
            knownMods.Count(mod => !mod.IsUnsupported),
            plan.EnabledMods.Length,
            knownMods.Count(mod => mod.IsUnsupported),
            selectedModSummary,
            plan.Fingerprint,
            phase,
            LauncherModLaunchReadinessCacheStatus.Fresh,
            plan
        );
    }

    private static LauncherModLaunchReadiness EvaluateVanilla(string phase)
    {
        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: vanilla mode selected",
            "runtime mod scan skipped"
        );
        return new LauncherModLaunchReadiness(
            LauncherModSelectionState.VanillaModeName,
            installedMods: 0,
            enabledMods: 0,
            unsupportedMods: 0,
            selectedMods: "<none>",
            modSetFingerprint: string.Empty,
            phase,
            LauncherModLaunchReadinessCacheStatus.NotNeededVanilla,
            LauncherModLaunchPlan.VanillaPlan()
        );
    }
}
