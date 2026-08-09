using Godot;
using System.Linq;
using STS2Mobile;
using STS2Mobile.Launcher;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void SetModsControlsVisible(bool visible)
    {
        _modsGroup.Visible = visible;
        _workshopSyncButton.Visible = visible;
        _workshopClearButton.Visible = visible;
        if (visible)
            ShowModsStartupSummary();
    }

    private void ShowModsStartupSummary()
    {
        var moddedMode = !LauncherPreviewMode.Enabled
            && LauncherModSelectionState.IsModdedMode;
        _readySummaryEnabledModCount = 0;
        _modsStatusLabel.Text = _compact
            ? moddedMode
                ? "Mods: selected | refresh from Mods controls"
                : "Mods: vanilla"
            : moddedMode
                ? "Play mode: Mods. Mod details load when you change mod mode or sync Workshop; Play remains available immediately."
                : "Play mode: Vanilla. Android Workshop and manual mod folders will not be scanned when the game starts.";
        RefreshModModeButtons(moddedMode, enabledCount: 0);
        RefreshModList(System.Array.Empty<LauncherKnownMod>());
        SetCompactWorkshopButtonText(activeCount: 0);
        UpdateBranchHelpText();
    }

    private void RefreshModsStatus()
    {
        PatchHelper.Log("[Launcher] Mods refresh phase: status start");
        var moddedMode = LauncherModSelectionState.IsModdedMode;
        if (!moddedMode)
        {
            _readySummaryEnabledModCount = 0;
            _modsStatusLabel.Text = BuildModsStatusText(
                activeCount: 0,
                externalManualCount: 0,
                unsupportedCount: 0,
                installedCount: 0,
                enabledCount: 0,
                moddedMode: false,
                unsupportedSummary: ""
            );
            RefreshModModeButtons(moddedMode, enabledCount: 0);
            RefreshModList(System.Array.Empty<LauncherKnownMod>());
            SetCompactWorkshopButtonText(activeCount: 0);
            UpdateBranchHelpText();
            PatchHelper.Log("[Launcher] Mods refresh phase: vanilla complete");
            return;
        }

        PatchHelper.Log("[Launcher] Mods refresh phase: scan known mods");
        var activeCount = LauncherWorkshopModSafety.ActiveStagedModCount();
        var externalManualCount = LauncherWorkshopModSafety.ExternalManualModPckCount();
        var unsupportedCount = LauncherWorkshopModSafety.UnsupportedWorkshopItemCount();
        var mods = LauncherModSelectionState.KnownMods();
        var installedCount = mods.Count(mod => !mod.IsUnsupported);
        var enabledCount = mods.Count(mod => mod.Enabled && !mod.IsUnsupported);
        _readySummaryEnabledModCount = enabledCount;
        _modsStatusLabel.Text = BuildModsStatusText(
            activeCount,
            externalManualCount,
            unsupportedCount,
            installedCount,
            enabledCount,
            moddedMode,
            LauncherWorkshopModSafety.UnsupportedWorkshopItemSummary()
        );
        RefreshModModeButtons(moddedMode, enabledCount);
        RefreshModList(mods);
        SetCompactWorkshopButtonText(activeCount);
        UpdateBranchHelpText();
        PatchHelper.Log("[Launcher] Mods refresh phase: modded complete");
    }

    private void SetCompactWorkshopButtonText(int activeCount)
    {
        SetCompactActionButtonText(
            _workshopSyncButton,
            _compact
                ? CompactSupportToolText("Sync Workshop", activeCount > 0 ? $"{activeCount} staged" : "None staged")
                : "Sync Workshop Mods"
        );
        SetCompactActionButtonText(
            _workshopClearButton,
            _compact
                ? CompactSupportToolText("Clear Staged", activeCount > 0 ? $"{activeCount} staged" : "None staged")
                : "Clear Staged Mods"
        );
    }

    private string BuildModsStatusText(
        int activeCount,
        int externalManualCount,
        int unsupportedCount,
        int installedCount,
        int enabledCount,
        bool moddedMode,
        string unsupportedSummary
    )
    {
        if (_compact)
        {
            var activeText = moddedMode
                ? $"{enabledCount}/{installedCount} enabled"
                : "vanilla";
            var issueText = unsupportedCount > 0
                ? $" | {unsupportedCount} needs import"
                : "";
            return $"Mods: {activeText}{issueText}";
        }

        if (!moddedMode)
            return "Play mode: Vanilla. Android Workshop and manual mod folders will not be scanned when the game starts.";

        if (unsupportedCount > 0)
        {
            var itemText = string.IsNullOrWhiteSpace(unsupportedSummary)
                ? $"{unsupportedCount} Workshop item(s)"
                : unsupportedSummary;
            return activeCount > 0 || externalManualCount > 0
                ? $"Mods staged: Workshop {activeCount}, manual {externalManualCount}. {unsupportedCount} subscribed item(s) need manual import: {itemText}. Put mod folders or PCK files in {AppPaths.ExternalModsDir}."
                : $"Mods need attention: {unsupportedCount} subscribed item(s) need manual import: {itemText}. Put mod folders or PCK files in {AppPaths.ExternalModsDir}.";
        }

        if (activeCount > 0 || externalManualCount > 0)
            return $"Play mode: Mods. Enabled {enabledCount} of {installedCount} installed mod(s). Workshop {activeCount} staged, manual {externalManualCount}.";

        return $"Mods: none staged or discovered. Sync Workshop or place manual mod folders/PCK files in {AppPaths.ExternalModsDir}.";
    }

    private void RefreshModModeButtons(bool moddedMode, int enabledCount)
    {
        ApplyToggle(_playVanillaButton, !moddedMode, _compact
            ? CompactSupportToolText("Play Vanilla", "No mods")
            : "Play Vanilla");
        ApplyToggle(_playModdedButton, moddedMode, _compact
            ? CompactSupportToolText("Play With Mods", $"{enabledCount} enabled")
            : "Play With Mods");
    }

    private void RefreshModList(System.Collections.Generic.IReadOnlyList<LauncherKnownMod> mods)
    {
        PatchHelper.Log("[Launcher] Mods refresh phase: update mod toggle slots");
        for (var i = 0; i < _modToggleButtons.Count; i++)
        {
            _modToggleKeys[i] = null;
            _modToggleCanChange[i] = false;
            _modToggleButtons[i].Visible = false;
        }

        var index = 0;
        foreach (var mod in mods.Take(MaxVisibleModToggles))
        {
            if (index >= _modToggleButtons.Count)
                break;

            var label = ModToggleText(mod);
            var button = _modToggleButtons[index];
            _modToggleKeys[index] = mod.Key;
            _modToggleCanChange[index] =
                !mod.IsUnsupported && !mod.IsRequiredDependency;
            button.Visible = true;
            ApplyToggle(button, mod.Enabled, label);
            index++;
        }

        ApplyContextControlsDisabled();

        PatchHelper.Log($"[Launcher] Mods refresh phase: update mod toggle slots complete count={index}");
    }

    private string ModToggleText(LauncherKnownMod mod)
    {
        var state = mod.IsDeprecated
            ? "Deprecated - native modded saves"
            : mod.IsUnsupported
                ? "Needs import"
            : mod.Enabled
                ? "Enabled"
                : "Disabled";
        var title = string.IsNullOrWhiteSpace(mod.Title) ? mod.Id : mod.Title;
        var detail = $"{mod.Source} | {state}";
        if (mod.IsRequiredDependency)
            detail += " | Required";
        if (mod.IsDependency)
            detail += " | Dependency";
        if (_compact)
            return CompactSupportToolText(title, detail);
        return $"{title}: {detail}";
    }

    private void SetModPlayMode(LauncherModPlayMode mode)
    {
        LauncherModSelectionState.SetPlayMode(mode);
        RefreshModsStatus();
        ModsSelectionChanged?.Invoke();
    }

    private void ToggleMod(string key, bool enabled)
    {
        LauncherModSelectionState.SetModEnabled(key, enabled);
        RefreshModsStatus();
        ModsSelectionChanged?.Invoke();
    }

    private void ToggleModAtIndex(int index)
    {
        if (index < 0 || index >= _modToggleKeys.Length)
            return;

        var key = _modToggleKeys[index];
        if (string.IsNullOrWhiteSpace(key))
            return;

        var mod = LauncherModSelectionState.KnownMods().FirstOrDefault(candidate => candidate.Key == key);
        if (mod == null || mod.IsUnsupported || mod.IsRequiredDependency)
            return;

        ToggleMod(key, !mod.Enabled);
    }
}
