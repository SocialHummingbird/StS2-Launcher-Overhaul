using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void BuildDestinationLayout()
    {
        GetChild<Control>(0).Visible = false;

        _homeDestination = BuildDestination(
            "Home",
            LauncherComponentTheme.OrangeAccent
        );
        _savesDestination = BuildDestination(
            "Saves",
            new Color(0.25f, 0.65f, 0.75f)
        );
        _versionsDestination = BuildDestination(
            "Versions",
            LauncherComponentTheme.CyanAccent
        );
        _modsDestination = BuildDestination(
            "Mods",
            new Color(0.72f, 0.46f, 0.9f)
        );
        _helpDestination = BuildDestination(
            "Help",
            LauncherComponentTheme.TextSecondary
        );

        MoveTo(_homeDestination, _homeJourney);
        MoveTo(_homeDestination, _launchButton);
        MoveTo(_homeDestination, _retryButton);
        _homeDestination.AddChild(BuildHomeShortcuts());

        MoveTo(_savesDestination, _saveSyncGroup);

        MoveTo(_versionsDestination, _versionSelectionGroup);
        MoveTo(_versionsDestination, _updateButton);
        MoveTo(_versionsDestination, _refreshVersionsButton);
        _versionMaintenanceGroup = BuildVersionMaintenanceGroup();
        _versionsDestination.AddChild(_versionMaintenanceGroup);

        MoveTo(_modsDestination, _modsGroup);

        _helpDestination.AddChild(BuildHelpRecoveryGuidance());
        MoveTo(_helpDestination, _safeLaunchButton);
        MoveTo(_helpDestination, _rendererGroup);
        var appUpdateLabel = new StyledLabel("App updates", _scale, fontSize: 14, align: HorizontalAlignment.Left);
        appUpdateLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        _helpDestination.AddChild(appUpdateLabel);
        var appUpdate = new StyledButton("Check for app updates", _scale, height: LauncherSectionMetrics.SecondaryButtonHeight)
        {
            Name = "CheckAppUpdates",
            TooltipText = "Check for a newer launcher APK. Download and install without opening GitHub.",
        };
        LauncherButtonStyles.ApplySupportAction(appUpdate, _scale);
        appUpdate.Pressed += () => CheckAppUpdatesPressed?.Invoke();
        _helpDestination.AddChild(appUpdate);
        _helpDestination.AddChild(BuildHelpDiagnosticsGroup());

        Visible = true;
        SetDestination(LauncherDestination.Home);
    }

    private VBoxContainer BuildVersionMaintenanceGroup()
    {
        var group = new VBoxContainer
        {
            Name = "SelectedVersionRepairGroup",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        group.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, _scale)
        );
        var title = new StyledLabel(
            "Selected version redownload",
            _scale,
            fontSize: _compact
                ? LauncherSectionMetrics.CompactVersionSummaryFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        title.Name = "SelectedVersionRepairLabel";
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(title);

        var guidance = new StyledLabel(
            "Redownload removes only the selected version's downloaded files. Saves and other versions stay in place.",
            _scale,
            fontSize: _compact ? 14 : 15,
            align: HorizontalAlignment.Left
        )
        {
            Name = "SelectedVersionRepairGuidance",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        guidance.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(guidance);

        var actions = new GridContainer
        {
            Name = "SelectedVersionRepairActions",
            Columns = 1,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        actions.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, _scale)
        );
        MoveTo(actions, _redownloadButton);
        group.AddChild(actions);
        return group;
    }

    private Label BuildHelpRecoveryGuidance()
    {
        var guidance = new StyledLabel(
            "If preparation requires a redownload, use Redownload selected version; do not uninstall or clear app data. If the game starts but does not appear, try Safe Start. Create a support report if either problem repeats.",
            _scale,
            fontSize: _compact ? 15 : 16,
            align: HorizontalAlignment.Left
        )
        {
            Name = "HelpRecoveryGuidance",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        guidance.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        return guidance;
    }

    private VBoxContainer BuildHelpDiagnosticsGroup()
    {
        var group = new VBoxContainer
        {
            Name = "HelpDiagnosticsGroup",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        group.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, _scale)
        );

        var title = new StyledLabel(
            "Diagnostics",
            _scale,
            fontSize: _compact
                ? LauncherSectionMetrics.CompactVersionSummaryFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        )
        {
            Name = "HelpDiagnosticsLabel",
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(title);

        var actions = new GridContainer
        {
            Name = "HelpDiagnosticsActions",
            Columns = _compactStackedActionRows ? 1 : 2,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        actions.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, _scale)
        );
        MoveTo(actions, _diagnosticsButton);
        var reportBug = new StyledButton("Report a bug on GitHub", _scale, height: LauncherSectionMetrics.SecondaryButtonHeight)
        {
            Name = "ReportBugOnGitHub",
            TooltipText = "Opens a new issue with a redacted log excerpt and copies the full redacted log. Review and submit on GitHub.",
            AccessibilityDescription = "Opens GitHub in your browser with launcher diagnostics prefilled. Nothing is submitted automatically.",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        LauncherButtonStyles.ApplySupportAction(reportBug, _scale);
        reportBug.Pressed += () => ReportBugPressed?.Invoke();
        actions.AddChild(reportBug);
        MoveTo(actions, _showLastErrorButton);
        MoveTo(actions, _copyRawLogButton);
        group.AddChild(actions);
        return group;
    }

    private void UpdateVersionMaintenanceVisibility()
    {
        if (_versionMaintenanceGroup == null)
            return;

        _refreshVersionsButton.Visible = true;
        _versionSelectionGroup.Visible = true;
        _versionMaintenanceGroup.Visible = true;
        UpdateVersionActionAvailability();
    }

    private VBoxContainer BuildDestination(string title, Color accent)
    {
        var destination = new VBoxContainer
        {
            Name = $"{title}Destination",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Visible = false,
        };
        destination.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(_compact ? 10 : 12, _scale)
        );

        var heading = new VBoxContainer
        {
            Name = $"{title}DestinationHeader",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        heading.AddThemeConstantOverride("separation", LauncherViewLayoutMetrics.ScaleInt(4, _scale));


        var titleLabel = new StyledLabel(
            title == "Home" ? "Slay the Spire 2" : title,
            _scale,
            fontSize: _compact ? 28 : 34,
            align: HorizontalAlignment.Left
        );
        titleLabel.Name = $"{title}DestinationTitle";
        titleLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        heading.AddChild(titleLabel);
        var description = title switch
        {
            "Home" => "Your next climb starts here.",
            "Saves" => "Keep your progress within reach.",
            "Versions" => "Choose the build you want to play.",
            "Mods" => "Make the next run your own.",
            _ => "Get back to your game.",
        };
        var subtitle = new StyledLabel(description, _scale, fontSize: 14, align: HorizontalAlignment.Left)
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        subtitle.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        heading.AddChild(subtitle);
        heading.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8 * _scale), MouseFilter = Control.MouseFilterEnum.Ignore });
        destination.AddChild(heading);
        AddChild(destination);
        return destination;
    }

    private static void MoveTo(Node destination, Node child)
    {
        child.GetParent()?.RemoveChild(child);
        destination.AddChild(child);
    }

    internal void SetDestination(LauncherDestination destination)
    {
        _destination = destination;
        ApplyDestinationVisibility();
    }

    private void ApplyDestinationVisibility()
    {
        _homeDestination.Visible = _destination == LauncherDestination.Home
            && (_launchButton.Visible || _retryButton.Visible);
        _savesDestination.Visible = _destination == LauncherDestination.Saves;
        _versionsDestination.Visible = _destination == LauncherDestination.Versions;
        _modsDestination.Visible = _destination == LauncherDestination.Mods;
        _helpDestination.Visible = _destination == LauncherDestination.Help;
    }
}
