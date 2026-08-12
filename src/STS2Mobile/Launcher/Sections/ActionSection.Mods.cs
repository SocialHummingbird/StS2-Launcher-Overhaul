using Godot;
using System.Linq;
using STS2Mobile;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ShowModsControls()
    {
        // Selection and discovery are local state. Keep them available even when
        // account, download, or mod loading state prevents a normal launch.
        _modsGroup.Visible = true;
        _workshopSyncButton.Visible = true;
        _workshopClearButton.Visible = true;
        ShowModsStartupSummary();
    }

    private void ShowModsStartupSummary()
        => RefreshModsStatus();

    private void RefreshModsStatus()
    {
        try
        {
            SetModsPresentation(LauncherModsPresentationState.ReadCurrent());
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to refresh Mods page: {ex.Message}");
            var selection = LauncherModSelectionState.Load();
            SetModsPresentation(
                LauncherModsPresentationState.Build(
                    selection,
                    System.Array.Empty<LauncherKnownMod>(),
                    marker: null
                )
            );
        }
    }

    internal void SetModsPresentation(LauncherModsPresentation presentation)
    {
        if (presentation == null)
            return;

        var moddedMode = presentation.Mode == LauncherModPlayMode.Modded;
        _readySummaryEnabledModCount = moddedMode ? presentation.EnabledCount : 0;
        _modsStatusLabel.Text = PresentationText(
            presentation.StatusText,
            presentation.Mods.Count == 0
                ? "No mods discovered."
                : $"{presentation.InstalledCount} installed; {presentation.EnabledCount} enabled."
        );
        RefreshModModeButtons(moddedMode, presentation.EnabledCount);
        ApplyModJourneyPresentation(
            moddedMode,
            presentation.EnabledCount,
            presentation.SaveNamespaceLabel,
            presentation.PrimaryPlayLabel
        );
        RefreshModList(presentation.Mods);
        SetCompactWorkshopButtonText(LauncherWorkshopModSafety.ActiveStagedModCount());
        UpdateBranchHelpText();
    }

    private void ApplyModJourneyPresentation(
        bool moddedMode,
        int enabledCount,
        string saveNamespace,
        string playLabel
    )
    {
        var modeName = moddedMode ? "Modded" : "Vanilla";
        saveNamespace = PresentationText(
            saveNamespace,
            moddedMode ? "Modded saves" : "Vanilla saves"
        );
        _modsSelectedModeLabel.Text = $"Selected mode: {modeName}";
        _modsSaveNamespaceLabel.Text = $"Next save set: {saveNamespace}";
        _homeSaveNamespaceState.Text = saveNamespace;
        _contextualPlayLabel = PresentationText(
            playLabel,
            moddedMode ? $"Play Modded · {enabledCount} enabled" : "Play Vanilla"
        );
        ApplyContextualPlayLabel();
    }

    private void ApplyContextualPlayLabel()
    {
        var text = _compact
            ? CompactPlaySyncDrawerText(_contextualPlayLabel, _homeSaveNamespaceState.Text)
            : _contextualPlayLabel;
        SetCompactActionButtonText(_launchButton, text);
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
                ? CompactSupportToolText("Clear staged...", activeCount > 0 ? $"{activeCount} staged" : "Confirmation")
                : "Clear staged mods..."
        );
    }

    private void RefreshModModeButtons(bool moddedMode, int enabledCount)
    {
        ApplyToggle(_playVanillaButton, !moddedMode, _compact
            ? CompactSupportToolText("Vanilla", !moddedMode ? "Selected" : "Choose")
            : !moddedMode ? "Vanilla · Selected" : "Vanilla");
        ApplyToggle(_playModdedButton, moddedMode, _compact
            ? CompactSupportToolText("Modded", moddedMode ? "Selected" : $"{enabledCount} enabled")
            : moddedMode ? "Modded · Selected" : "Modded");
    }

    private void RefreshModList(
        System.Collections.Generic.IReadOnlyList<LauncherModPresentationItem> mods
    )
    {
        PatchHelper.Log("[Launcher] Mods refresh phase: update truthful mod rows");
        var visibleMods = (mods ?? System.Array.Empty<LauncherModPresentationItem>())
            .ToArray();
        EnsureModToggleSlots(visibleMods.Length);
        for (var i = 0; i < _modToggleButtons.Count; i++)
        {
            _modToggleKeys[i] = null;
            _modToggleCanChange[i] = false;
            _modToggleButtons[i].Visible = false;
            _modToggleButtons[i].TooltipText = "";
            _modToggleButtons[i].AccessibilityDescription = "";
        }

        var index = 0;
        foreach (var mod in visibleMods)
        {
            if (index >= _modToggleButtons.Count)
                break;

            var label = ModToggleText(mod);
            var button = _modToggleButtons[index];
            _modToggleKeys[index] = mod.Key;
            _modToggleCanChange[index] = mod.CanChange;
            button.Visible = true;
            ApplyToggle(button, mod.Enabled, label);
            button.TooltipText = PresentationText(mod.Detail, label.Replace('\n', ' '));
            button.AccessibilityDescription = button.TooltipText;
            index++;
        }

        ApplyContextControlsDisabled();
        PatchHelper.Log($"[Launcher] Mods refresh phase: truthful mod rows complete count={index}");
    }

    private void EnsureModToggleSlots(int count)
    {
        while (_modToggleButtons.Count < count)
        {
            var slot = _modToggleButtons.Count;
            var button = AddActionButton(
                _modsList,
                "",
                _scale,
                () => ToggleModAtIndex(slot)
            );
            button.Visible = false;
            LauncherButtonStyles.ApplySupportAction(button, _scale);
            _modToggleButtons.Add(button);
            _modToggleKeys.Add(null);
            _modToggleCanChange.Add(false);
        }
    }

    private string ModToggleText(LauncherModPresentationItem mod)
    {
        var title = string.IsNullOrWhiteSpace(mod.Title) ? mod.Id : mod.Title;
        var installedState = mod.Installed ? "Installed" : "Not installed";
        var enabledState = mod.Enabled ? "Enabled for Modded" : "Disabled for Modded";
        var lastLaunchState = mod.IsLastLaunchStale
            ? "Not tested yet"
            : mod.LastLaunchState switch
            {
                LauncherModLastLaunchState.LoadedLastLaunch => "Loaded last launch",
                LauncherModLastLaunchState.Partial => "Partial",
                LauncherModLastLaunchState.Failed => "Failed",
                _ => "Not tested yet",
            };
        var source = string.IsNullOrWhiteSpace(mod.Source) ? "Unknown source" : mod.Source;
        var detail = $"{source} · {installedState} · {enabledState} · {lastLaunchState}";
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
        if (index < 0 || index >= _modToggleKeys.Count)
            return;

        var key = _modToggleKeys[index];
        if (string.IsNullOrWhiteSpace(key) || !_modToggleCanChange[index])
            return;

        var selection = LauncherModSelectionState.Load();
        var selected = selection.EnabledMods.TryGetValue(key, out var enabled) && enabled;
        ToggleMod(key, !selected);
    }
}
