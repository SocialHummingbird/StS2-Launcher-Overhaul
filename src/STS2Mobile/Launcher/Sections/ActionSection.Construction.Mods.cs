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
            Label statusLabel,
            VBoxContainer modsList,
            Button workshopSyncButton,
            Button workshopClearButton
        )
        {
            Group = group;
            PlayVanillaButton = playVanillaButton;
            PlayModdedButton = playModdedButton;
            StatusLabel = statusLabel;
            ModsList = modsList;
            WorkshopSyncButton = workshopSyncButton;
            WorkshopClearButton = workshopClearButton;
        }

        internal VBoxContainer Group { get; }
        internal Button PlayVanillaButton { get; }
        internal Button PlayModdedButton { get; }
        internal Label StatusLabel { get; }
        internal VBoxContainer ModsList { get; }
        internal Button WorkshopSyncButton { get; }
        internal Button WorkshopClearButton { get; }
    }

    private ModsControls BuildModsControls(float scale, bool compact)
    {
        var group = BuildActionGroup(scale);
        group.Visible = false;

        Container modeParent = compact && !_compactStackedActionRows
            ? BuildCompactCloudPrimaryActionsRow(group, scale, compactStackedActionRows: false)
            : group;

        var playVanillaButton = AddPushPullButton(
            modeParent,
            compact ? CompactSupportToolText("Play Vanilla", "No mods") : "Play Vanilla",
            scale,
            () => SetModPlayMode(LauncherModPlayMode.Vanilla)
        );
        LauncherButtonStyles.ApplySupportAction(playVanillaButton, scale);

        var playModdedButton = AddPushPullButton(
            modeParent,
            compact ? CompactSupportToolText("Play With Mods", "Selected") : "Play With Mods",
            scale,
            () => SetModPlayMode(LauncherModPlayMode.Modded)
        );
        LauncherButtonStyles.ApplyCloudPullAction(playModdedButton, scale);

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
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        modsList.AddThemeConstantOverride("separation", Math.Max(3, (int)(4 * scale)));
        group.AddChild(modsList);

        Container actionsParent = compact && !_compactStackedActionRows
            ? BuildCompactCloudPrimaryActionsRow(group, scale, compactStackedActionRows: false)
            : group;

        var workshopSyncButton = AddPushPullButton(
            actionsParent,
            compact ? CompactSupportToolText("Sync Workshop", "Mods") : "Sync Workshop Mods",
            scale,
            () => WorkshopSyncPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyCloudPullAction(workshopSyncButton, scale);
        SetCompactActionButtonText(workshopSyncButton, workshopSyncButton.Text);

        var workshopClearButton = AddPushPullButton(
            actionsParent,
            compact ? CompactSupportToolText("Clear Staged", "Keep files") : "Clear Staged Mods",
            scale,
            () => WorkshopClearPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(workshopClearButton, scale);
        SetCompactActionButtonText(workshopClearButton, workshopClearButton.Text);

        AddChild(group);
        return new ModsControls(
            group,
            playVanillaButton,
            playModdedButton,
            statusLabel,
            modsList,
            workshopSyncButton,
            workshopClearButton
        );
    }
}
