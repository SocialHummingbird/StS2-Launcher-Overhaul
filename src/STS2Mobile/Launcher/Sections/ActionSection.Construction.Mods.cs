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
            Label launchSummaryLabel,
            VBoxContainer modsList,
            Button workshopSyncButton,
            Button workshopClearButton
        )
        {
            Group = group;
            PlayVanillaButton = playVanillaButton;
            PlayModdedButton = playModdedButton;
            LaunchSummaryLabel = launchSummaryLabel;
            ModsList = modsList;
            WorkshopSyncButton = workshopSyncButton;
            WorkshopClearButton = workshopClearButton;
        }

        internal VBoxContainer Group { get; }
        internal Button PlayVanillaButton { get; }
        internal Button PlayModdedButton { get; }
        internal Label LaunchSummaryLabel { get; }
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
            "Vanilla",
            scale,
            () => SetModPlayMode(LauncherModPlayMode.Vanilla)
        );
        LauncherButtonStyles.ApplySupportAction(playVanillaButton, scale);

        var playModdedButton = AddActionButton(
            modeParent,
            "Modded",
            scale,
            () => SetModPlayMode(LauncherModPlayMode.Modded)
        );
        LauncherButtonStyles.ApplySupportAction(playModdedButton, scale);

        var launchSummaryLabel = new StyledLabel(
            "Uses Vanilla saves",
            scale,
            fontSize: compact ? 14 : 15,
            align: HorizontalAlignment.Left
        )
        {
            Name = "ModsLaunchSummary",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        launchSummaryLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        group.AddChild(launchSummaryLabel);

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
            "Update Workshop mods",
            scale,
            () => WorkshopSyncPressed?.Invoke()
        );
        workshopSyncButton.AccessibilityName = "Update Workshop mods";
        LauncherButtonStyles.ApplyPrimaryAction(workshopSyncButton, scale);
        SetCompactActionButtonText(workshopSyncButton, workshopSyncButton.Text);

        var workshopClearButton = AddActionButton(
            actionsParent,
            "Remove downloaded Workshop mods\u2026",
            scale,
            () => WorkshopClearPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(workshopClearButton, scale);
        workshopClearButton.AccessibilityName = "Remove downloaded Workshop mods\u2026";
        workshopClearButton.TooltipText = "Review before removing downloaded Workshop mods.";
        workshopClearButton.AccessibilityDescription = workshopClearButton.TooltipText;
        SetCompactActionButtonText(workshopClearButton, workshopClearButton.Text);

        AddChild(group);
        return new ModsControls(
            group,
            playVanillaButton,
            playModdedButton,
            launchSummaryLabel,
            modsList,
            workshopSyncButton,
            workshopClearButton
        );
    }

}
