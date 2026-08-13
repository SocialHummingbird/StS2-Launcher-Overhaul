using Godot;
using System.Linq;
using STS2Mobile;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private sealed class ModRowControls
    {
        internal ModRowControls(
            PanelContainer container,
            Label title,
            Label source,
            Label runtimeResult,
            CheckButton enabledToggle
        )
        {
            Container = container;
            Title = title;
            Source = source;
            RuntimeResult = runtimeResult;
            EnabledToggle = enabledToggle;
        }

        internal PanelContainer Container { get; }
        internal Label Title { get; }
        internal Label Source { get; }
        internal Label RuntimeResult { get; }
        internal CheckButton EnabledToggle { get; }
        internal string Key { get; set; }
        internal bool CanChange { get; set; }
    }

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
        var enabledModCount = presentation.EnabledCount == 1
            ? "1 mod"
            : $"{presentation.EnabledCount} mods";
        _modsLaunchSummaryLabel.Text = moddedMode
            ? $"Uses Modded saves \u00B7 {enabledModCount} enabled"
            : "Uses Vanilla saves";
        RefreshModModeButtons(moddedMode, presentation.EnabledCount);
        ApplyModJourneyPresentation(
            moddedMode,
            presentation.EnabledCount,
            presentation.SaveNamespaceLabel
        );
        RefreshModList(presentation.Mods);
        UpdateBranchHelpText();
    }

    private void ApplyModJourneyPresentation(
        bool moddedMode,
        int enabledCount,
        string saveNamespace
    )
    {
        saveNamespace = PresentationText(
            saveNamespace,
            moddedMode ? "Modded saves" : "Vanilla saves"
        );
        _homeSaveNamespace = saveNamespace;
        UpdateHomeStateLine();
        _contextualPlayLabel = moddedMode
            ? ModdedPlayLabel(enabledCount)
            : "Play Vanilla";
        ApplyContextualPlayLabel();
    }

    private void ApplyContextualPlayLabel()
    {
        SetCompactActionButtonText(_launchButton, _contextualPlayLabel);
    }

    private static string ModdedPlayLabel(int enabledCount)
        => $"Play Modded \u00B7 {enabledCount} {(enabledCount == 1 ? "mod" : "mods")}";

    private void RefreshModModeButtons(bool moddedMode, int enabledCount)
    {
        ApplyToggle(_playVanillaButton, !moddedMode, _compact
            ? "Vanilla"
            : !moddedMode ? "Vanilla \u00B7 Selected" : "Vanilla");
        ApplyToggle(_playModdedButton, moddedMode, _compact
            ? "Modded"
            : moddedMode ? "Modded \u00B7 Selected" : "Modded");
        _playVanillaButton.AccessibilityName = "Vanilla";
        _playVanillaButton.AccessibilityDescription = !moddedMode ? "Selected mode" : "";
        _playModdedButton.AccessibilityName = "Modded";
        _playModdedButton.AccessibilityDescription = moddedMode ? "Selected mode" : "";
    }

    private void RefreshModList(
        System.Collections.Generic.IReadOnlyList<LauncherModPresentationItem> mods
    )
    {
        PatchHelper.Log("[Launcher] Mods refresh phase: update truthful mod rows");
        var visibleMods = (mods ?? System.Array.Empty<LauncherModPresentationItem>())
            .ToArray();
        EnsureModToggleSlots(visibleMods.Length);
        for (var i = 0; i < _modRows.Count; i++)
        {
            var row = _modRows[i];
            row.Key = null;
            row.CanChange = false;
            row.Container.Visible = false;
            row.Container.TooltipText = "";
            row.EnabledToggle.AccessibilityDescription = "";
        }

        var index = 0;
        foreach (var mod in visibleMods)
        {
            if (index >= _modRows.Count)
                break;

            var row = _modRows[index];
            var title = string.IsNullOrWhiteSpace(mod.Title) ? mod.Id : mod.Title;
            var source = string.IsNullOrWhiteSpace(mod.Source) ? "Unknown source" : mod.Source;
            if (!mod.Installed)
                source += " \u00B7 files missing";
            var result = RuntimeResultText(mod);

            row.Key = mod.Key;
            row.CanChange = mod.CanChange;
            row.Container.Visible = true;
            row.Title.Text = title;
            row.Source.Text = source;
            row.RuntimeResult.Text = result;
            row.EnabledToggle.ButtonPressed = mod.Enabled;
            row.EnabledToggle.AccessibilityName = title;
            var detail = PresentationText(mod.Detail, result);
            row.EnabledToggle.AccessibilityDescription =
                $"{(mod.Enabled ? "Enabled" : "Disabled")} for Modded mode. "
                + $"{source}. {result}. {detail}";
            row.Container.TooltipText = detail;
            index++;
        }

        ApplyContextControlsDisabled();
        PatchHelper.Log($"[Launcher] Mods refresh phase: truthful mod rows complete count={index}");
    }

    private void EnsureModToggleSlots(int count)
    {
        while (_modRows.Count < count)
        {
            var slot = _modRows.Count;
            var panel = new PanelContainer
            {
                Name = "ModRow",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Visible = false,
            };
            panel.AddThemeStyleboxOverride(
                LauncherComponentTheme.Panel,
                LauncherStyleBoxes.MakeOutline(
                    LauncherComponentTheme.CyanDim,
                    LauncherViewLayoutMetrics.ScaleInt(6, _scale),
                    1
                )
            );

            var margin = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            var horizontalMargin = LauncherViewLayoutMetrics.ScaleInt(10, _scale);
            var verticalMargin = LauncherViewLayoutMetrics.ScaleInt(7, _scale);
            margin.AddThemeConstantOverride("margin_left", horizontalMargin);
            margin.AddThemeConstantOverride("margin_right", horizontalMargin);
            margin.AddThemeConstantOverride("margin_top", verticalMargin);
            margin.AddThemeConstantOverride("margin_bottom", verticalMargin);
            panel.AddChild(margin);

            var content = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            content.AddThemeConstantOverride(
                "separation",
                LauncherViewLayoutMetrics.ScaleInt(3, _scale)
            );
            margin.AddChild(content);

            var header = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            header.AddThemeConstantOverride(
                "separation",
                LauncherViewLayoutMetrics.ScaleInt(8, _scale)
            );
            content.AddChild(header);

            var title = new StyledLabel(
                "",
                _scale,
                fontSize: _compact ? 14 : 15,
                align: HorizontalAlignment.Left
            )
            {
                Name = "ModName",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            title.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                LauncherComponentTheme.TextPrimary
            );
            header.AddChild(title);

            var enabledToggle = new CheckButton
            {
                Name = "ModEnabledToggle",
                Text = "Enabled",
                ToggleMode = true,
                FocusMode = Control.FocusModeEnum.All,
            };
            enabledToggle.AddThemeFontSizeOverride(
                LauncherComponentTheme.FontSize,
                LauncherViewLayoutMetrics.ScaleInt(_compact ? 13 : 14, _scale)
            );
            enabledToggle.Pressed += () => ToggleModAtIndex(slot);
            header.AddChild(enabledToggle);

            var source = new StyledLabel(
                "",
                _scale,
                fontSize: _compact ? 11 : 12,
                align: HorizontalAlignment.Left
            )
            {
                Name = "ModSource",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            source.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                LauncherComponentTheme.TextMuted
            );
            content.AddChild(source);

            var runtimeResult = new StyledLabel(
                "",
                _scale,
                fontSize: _compact ? 12 : 13,
                align: HorizontalAlignment.Left
            )
            {
                Name = "ModRuntimeResult",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            runtimeResult.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                LauncherComponentTheme.TextSecondary
            );
            content.AddChild(runtimeResult);

            _modsList.AddChild(panel);
            _modRows.Add(new ModRowControls(panel, title, source, runtimeResult, enabledToggle));
        }
    }

    private static string RuntimeResultText(LauncherModPresentationItem mod)
        => mod.IsLastLaunchStale
            ? "Not run with this setup"
            : mod.LastLaunchState switch
            {
                LauncherModLastLaunchState.LoadedLastLaunch => "Active last launch",
                LauncherModLastLaunchState.Partial => "Partly loaded",
                LauncherModLastLaunchState.Failed => "Failed last launch",
                _ => "Not run with this setup",
            };

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
        if (index < 0 || index >= _modRows.Count)
            return;

        var row = _modRows[index];
        var key = row.Key;
        if (string.IsNullOrWhiteSpace(key) || !row.CanChange)
            return;

        var selection = LauncherModSelectionState.Load();
        var selected = selection.EnabledMods.TryGetValue(key, out var enabled) && enabled;
        ToggleMod(key, !selected);
    }
}
