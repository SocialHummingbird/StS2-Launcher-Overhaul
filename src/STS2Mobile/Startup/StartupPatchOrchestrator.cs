using System;
using System.Collections.Generic;
using HarmonyLib;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static class StartupPatchOrchestrator
{
    private readonly record struct PatchStep(string Name, Action<Harmony> Apply);
    private readonly record struct PatchGroup(string Name, bool Critical, IReadOnlyList<PatchStep> Steps);

    internal sealed record PatchGroupResult(
        string Name,
        int Applied,
        int Total,
        IReadOnlyList<string> Failures
    );

    private static readonly PatchGroup[] Groups = new[]
    {
        new(
            "core",
            true,
            new[]
            {
                new PatchStep("Model DB bootstrap", ModelDbInitPatch.Apply),
                new PatchStep("Platform compatibility", PlatformPatches.Apply),
            }
        ),
        new(
            "gameplay",
            false,
            new[]
            {
                new PatchStep("Settings compatibility", SettingsPatches.Apply),
                new PatchStep("UI scaling", UiScalePatches.Apply),
                new PatchStep("Mobile layout", MobileLayoutPatches.Apply),
                new PatchStep("Event layout", EventLayoutPatches.Apply),
                new PatchStep("Merchant layout", MerchantLayoutPatches.Apply),
                new PatchStep("App lifecycle", AppLifecyclePatches.Apply),
                new PatchStep("Touch input", TouchInputPatches.Apply),
                new PatchStep("Card reward", CardRewardPatches.Apply),
                new PatchStep("Early access disclaimer", EarlyAccessDisclaimerPatches.Apply),
                new PatchStep("Combat background", CombatBackgroundPatches.Apply),
            }
        ),
        new(
            "optional",
            false,
            new[]
            {
                new PatchStep("LAN multiplayer", LanMultiplayerPatcher.Apply),
                new PatchStep("Mod loader integration", ModLoaderPatches.Apply),
                new PatchStep("Launcher UI", LauncherPatches.Apply),
                new PatchStep("Save diagnostics", SaveDiagnosticPatches.Apply),
            }
        )
    };

    internal static StartupPatchResult Apply(Harmony harmony)
    {
        var results = new List<PatchGroupResult>(Groups.Length);
        var criticalFailed = false;

        foreach (var group in Groups)
        {
            var result = ApplyGroup(group, harmony);
            results.Add(result);

            if (group.Critical && result.Failures.Count > 0)
            {
                criticalFailed = true;
                break;
            }
        }

        return new StartupPatchResult(results, criticalFailed);
    }

    private static PatchGroupResult ApplyGroup(PatchGroup group, Harmony harmony)
    {
        var failures = new List<string>();
        var applied = 0;

        for (var i = 0; i < group.Steps.Count; i++)
        {
            var step = group.Steps[i];
            try
            {
                step.Apply(harmony);
                applied++;
            }
            catch (Exception ex)
            {
                failures.Add($"{step.Name}: {ex.Message}");
                PatchHelper.Log($"[{group.Name}] {step.Name} failed: {ex.Message}");
            }
        }

        PatchHelper.Log($"[{group.Name}] {applied}/{group.Steps.Count} patches applied, {failures.Count} failed");
        return new PatchGroupResult(group.Name, applied, group.Steps.Count, failures);
    }

    public sealed record StartupPatchResult(
        IReadOnlyList<PatchGroupResult> GroupResults,
        bool CriticalFailed
    );
}
