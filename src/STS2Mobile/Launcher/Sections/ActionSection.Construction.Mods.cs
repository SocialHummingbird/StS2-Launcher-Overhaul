using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly struct ModsControls
    {
        internal ModsControls(
            VBoxContainer group,
            Button playVanillaButton,
            Button playModdedButton,
            Label selectedModeLabel,
            Label saveNamespaceLabel,
            Label statusLabel,
            VBoxContainer modsList,
            Button workshopSyncButton,
            Button workshopClearButton
        )
        {
            Group = group;
            PlayVanillaButton = playVanillaButton;
            PlayModdedButton = playModdedButton;
            SelectedModeLabel = selectedModeLabel;
            SaveNamespaceLabel = saveNamespaceLabel;
            StatusLabel = statusLabel;
            ModsList = modsList;
            WorkshopSyncButton = workshopSyncButton;
            WorkshopClearButton = workshopClearButton;
        }

        internal VBoxContainer Group { get; }
        internal Button PlayVanillaButton { get; }
        internal Button PlayModdedButton { get; }
        internal Label SelectedModeLabel { get; }
        internal Label SaveNamespaceLabel { get; }
        internal Label StatusLabel { get; }
        internal VBoxContainer ModsList { get; }
        internal Button WorkshopSyncButton { get; }
        internal Button WorkshopClearButton { get; }
    }

    private ModsControls BuildModsControls(float scale, bool compact)
    {
        var group = BuildActionGroup(scale);
        group.Name = "ModsControls";
        group.Visible = false;

        Container modeParent = compact && !_compactStackedActionRows
            ? BuildCompactActionRow(group, scale, compactStackedActionRows: false)
            : group;

        var playVanillaButton = AddActionButton(
            modeParent,
            compact ? CompactSupportToolText("Vanilla", "Selector") : "Vanilla",
            scale,
            () => SetModPlayMode(LauncherModPlayMode.Vanilla)
        );
        LauncherButtonStyles.ApplySupportAction(playVanillaButton, scale);

        var playModdedButton = AddActionButton(
            modeParent,
            compact ? CompactSupportToolText("Modded", "Selector") : "Modded",
            scale,
            () => SetModPlayMode(LauncherModPlayMode.Modded)
        );
        LauncherButtonStyles.ApplyAccentAction(playModdedButton, scale);

        var selectedModeLabel = new StyledLabel(
            "Selected mode: Vanilla",
            scale,
            fontSize: compact ? 14 : 15,
            align: HorizontalAlignment.Left
        )
        {
            Name = "SelectedModMode",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        selectedModeLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        group.AddChild(selectedModeLabel);

        var saveNamespaceLabel = new StyledLabel(
            "Next save set: Vanilla saves",
            scale,
            fontSize: compact ? 12 : 13,
            align: HorizontalAlignment.Left
        )
        {
            Name = "NextSaveNamespace",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        saveNamespaceLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(saveNamespaceLabel);

        var statusLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactVersionSummaryFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusLabel.VerticalAlignment = VerticalAlignment.Center;
        statusLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(statusLabel);

        var modsList = new VBoxContainer
        {
            Name = "ModsList",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        modsList.AddThemeConstantOverride("separation", Math.Max(3, (int)(4 * scale)));
        group.AddChild(modsList);

        Container actionsParent = compact && !_compactStackedActionRows
            ? BuildCompactActionRow(group, scale, compactStackedActionRows: false)
            : group;

        var workshopSyncButton = AddActionButton(
            actionsParent,
            compact ? CompactSupportToolText("Sync Workshop", "Mods") : "Sync Workshop Mods",
            scale,
            () => WorkshopSyncPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyAccentAction(workshopSyncButton, scale);
        SetCompactActionButtonText(workshopSyncButton, workshopSyncButton.Text);

        var workshopClearButton = AddActionButton(
            actionsParent,
            compact ? CompactSupportToolText("Clear staged...", "Confirmation") : "Clear staged mods...",
            scale,
            () => WorkshopClearPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(workshopClearButton, scale);
        workshopClearButton.TooltipText = "Review before removing staged Workshop mods.";
        workshopClearButton.AccessibilityDescription = workshopClearButton.TooltipText;
        SetCompactActionButtonText(workshopClearButton, workshopClearButton.Text);

        AddChild(group);
        return new ModsControls(
            group,
            playVanillaButton,
            playModdedButton,
            selectedModeLabel,
            saveNamespaceLabel,
            statusLabel,
            modsList,
            workshopSyncButton,
            workshopClearButton
        );
    }

}
