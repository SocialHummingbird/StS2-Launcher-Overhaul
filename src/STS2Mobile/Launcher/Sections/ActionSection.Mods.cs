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
            RefreshModsStatus();
    }

    private void RefreshModsStatus()
    {
        var activeCount = LauncherWorkshopModSafety.ActiveStagedModCount();
        var externalManualCount = LauncherWorkshopModSafety.ExternalManualModPckCount();
        var unsupportedCount = LauncherWorkshopModSafety.UnsupportedWorkshopItemCount();
        var installedCount = LauncherModSelectionState.InstalledModCount();
        var enabledCount = LauncherModSelectionState.EnabledModCount();
        var moddedMode = LauncherModSelectionState.IsModdedMode;
        _modsStatusLabel.Text = BuildModsStatusText(
            activeCount,
            externalManualCount,
            unsupportedCount,
            installedCount,
            enabledCount,
            moddedMode,
            LauncherWorkshopModSafety.UnsupportedWorkshopItemSummary()
        );
        RefreshModModeButtons(moddedMode);
        RefreshModList();

        SetCompactActionButtonText(
            _workshopSyncButton,
            _compact
                ? CompactSupportToolText("Sync Workshop", activeCount > 0 ? $"{activeCount} active" : "No active")
                : "Sync Workshop Mods"
        );
        SetCompactActionButtonText(
            _workshopClearButton,
            _compact
                ? CompactSupportToolText("Clear Staged", activeCount > 0 ? $"{activeCount} active" : "No active")
                : "Clear Staged Mods"
        );
        UpdateBranchHelpText();
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
            var cloudText = moddedMode && enabledCount > 0
                ? " | Cloud upload locked"
                : "";
            return $"Mods: {activeText}{issueText}{cloudText}";
        }

        if (!moddedMode)
            return "Play mode: Vanilla. Android Workshop and manual mod folders will not be scanned when the game starts.";

        if (unsupportedCount > 0)
        {
            var itemText = string.IsNullOrWhiteSpace(unsupportedSummary)
                ? $"{unsupportedCount} Workshop item(s)"
                : unsupportedSummary;
            return activeCount > 0 || externalManualCount > 0
                ? $"Mods active: Workshop {activeCount}, manual {externalManualCount}. {unsupportedCount} subscribed item(s) need manual import: {itemText}. Put mod folders or PCK files in {AppPaths.ExternalModsDir}. Steam Cloud upload stays locked while staged Workshop mods are active."
                : $"Mods need attention: {unsupportedCount} subscribed item(s) need manual import: {itemText}. Put mod folders or PCK files in {AppPaths.ExternalModsDir}.";
        }

        if (activeCount > 0 || externalManualCount > 0)
            return $"Play mode: Mods. Enabled {enabledCount} of {installedCount} installed mod(s). Workshop {activeCount}, manual {externalManualCount}. Steam Cloud upload stays locked while selected mods are active.";

        return $"Mods: none active. Sync Workshop or place manual mod folders/PCK files in {AppPaths.ExternalModsDir}.";
    }

    private void RefreshModModeButtons(bool moddedMode)
    {
        ApplyToggle(_playVanillaButton, !moddedMode, _compact
            ? CompactSupportToolText("Play Vanilla", "No mods")
            : "Play Vanilla");
        ApplyToggle(_playModdedButton, moddedMode, _compact
            ? CompactSupportToolText("Play With Mods", $"{LauncherModSelectionState.EnabledModCount()} enabled")
            : "Play With Mods");
    }

    private void RefreshModList()
    {
        foreach (var child in _modsList.GetChildren().Cast<Node>().ToArray())
        {
            _modsList.RemoveChild(child);
            child.QueueFree();
        }

        var mods = LauncherModSelectionState.KnownMods();
        if (mods.Count == 0)
            return;

        foreach (var mod in mods.Take(12))
        {
            var label = ModToggleText(mod);
            var button = AddPushPullButton(
                _modsList,
                label,
                _scale,
                () => ToggleMod(mod.Key, !mod.Enabled)
            );
            ApplyToggle(button, mod.Enabled, label);
            button.Disabled = mod.IsUnsupported || mod.IsRequiredDependency;
        }
    }

    private string ModToggleText(LauncherKnownMod mod)
    {
        var state = mod.IsUnsupported
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
}
