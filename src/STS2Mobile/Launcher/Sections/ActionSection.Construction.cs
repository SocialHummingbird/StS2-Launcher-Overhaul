using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal ActionSection(float scale)
    {
        var toggleRadius = (int)(4 * scale);
        var toggleBorderWidth = Math.Max(1, (int)(2 * scale));
        _toggleOffStyle = LauncherStyleBoxes.MakeOutline(
            new Color(0.7f, 0.25f, 0.25f),
            toggleRadius,
            toggleBorderWidth
        );
        _toggleOnStyle = LauncherStyleBoxes.MakeOutline(
            new Color(0.25f, 0.65f, 0.3f),
            toggleRadius,
            toggleBorderWidth
        );

        _retryButton = AddHiddenButton(
            "RETRY",
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            () => RetryPressed?.Invoke()
        );

        _localBackupToggle = AddSecondaryHiddenButton("Local Backup: OFF", scale, null);
        _cloudSyncToggle = AddSecondaryHiddenButton("Auto Sync: OFF", scale, null);
        ConfigureLocalBackupToggle();
        ConfigureCloudSyncToggle();

        _pushPullRow = new HBoxContainer();
        _pushPullRow.Visible = false;
        _pushPullRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.PushPullRowSeparation, scale)
        );
        _pushButton = AddPushPullButton(
            _pushPullRow,
            "Push to Cloud",
            scale,
            () => CloudPushPressed?.Invoke()
        );
        _pullButton = AddPushPullButton(
            _pushPullRow,
            "Pull from Cloud",
            scale,
            () => CloudPullPressed?.Invoke()
        );
        AddChild(_pushPullRow);

        _updateButton = AddPrimaryHiddenButton(
            "CHECK FOR UPDATES",
            scale,
            () => CheckForUpdatesPressed?.Invoke()
        );
        _redownloadButton = AddSecondaryHiddenButton(
            "REDOWNLOAD GAME FILES",
            scale,
            () => RedownloadPressed?.Invoke()
        );
        _diagnosticsButton = AddSecondaryHiddenButton(
            "EXPORT DIAGNOSTICS",
            scale,
            () => DiagnosticsPressed?.Invoke()
        );
        _showLastErrorButton = AddSecondaryHiddenButton(
            "SHOW LAST ERROR",
            scale,
            () => ShowLastErrorPressed?.Invoke()
        );
        _copyRawLogButton = AddSecondaryHiddenButton(
            "COPY RAW ERROR LOG",
            scale,
            () => CopyRawLogPressed?.Invoke()
        );
        _safeLaunchButton = AddSecondaryHiddenButton(
            "SAFE LAUNCH",
            scale,
            () => SafeLaunchPressed?.Invoke()
        );
        _launchButton = AddPrimaryHiddenButton(
            "LAUNCH",
            scale,
            () => LaunchPressed?.Invoke()
        );
    }
}
