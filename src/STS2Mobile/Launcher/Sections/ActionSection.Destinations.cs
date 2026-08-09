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
            "Check status, then play.",
            LauncherComponentTheme.OrangeAccent
        );
        _savesDestination = BuildDestination(
            "Saves",
            "Automatic sync or choose a direction.",
            new Color(0.25f, 0.65f, 0.75f)
        );
        _versionsDestination = BuildDestination(
            "Versions",
            "Choose or repair game files.",
            LauncherComponentTheme.CyanAccent
        );
        _modsDestination = BuildDestination(
            "Mods",
            "Choose vanilla or mods.",
            new Color(0.72f, 0.46f, 0.9f)
        );
        _helpDestination = BuildDestination(
            "Help",
            "Compatibility, repair, and reports.",
            LauncherComponentTheme.TextSecondary
        );

        MoveTo(_homeDestination, _homeJourney);
        MoveTo(_homeDestination, _launchButton);
        MoveTo(_homeDestination, _readyVersionSummaryPanel);
        MoveTo(_homeDestination, _retryButton);

        MoveTo(_savesDestination, _saveSyncGroup);

        MoveTo(_versionsDestination, _branchDropdown);
        MoveTo(_versionsDestination, _branchDetailsToggle);
        MoveTo(_versionsDestination, _branchHelpLabel);
        MoveTo(_versionsDestination, _updateButton);
        MoveTo(_versionsDestination, _refreshVersionsButton);
        MoveTo(_versionsDestination, _redownloadButton);
        MoveTo(_versionsDestination, _clearCachedVersionsButton);

        MoveTo(_modsDestination, _modsGroup);

        MoveTo(_helpDestination, _safeLaunchButton);
        MoveTo(_helpDestination, _rendererGroup);
        MoveTo(_helpDestination, _diagnosticsButton);
        MoveTo(_helpDestination, _showLastErrorButton);
        MoveTo(_helpDestination, _copyRawLogButton);

        _supportToggle.Visible = false;
        _supportGroup.Visible = false;
        Visible = true;
        SetDestination(LauncherDestination.Home);
    }

    private VBoxContainer BuildDestination(string title, string subtitle, Color accent)
    {
        var destination = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Visible = false,
        };
        destination.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(_compact ? 10 : 12, _scale)
        );

        var heading = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        heading.AddThemeConstantOverride("separation", LauncherViewLayoutMetrics.ScaleInt(4, _scale));
        heading.AddChild(new ColorRect
        {
            Color = accent,
            CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(3, _scale)),
        });

        var titleLabel = new StyledLabel(
            title,
            _scale,
            fontSize: _compact ? 18 : 20,
            align: HorizontalAlignment.Left
        );
        titleLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        heading.AddChild(titleLabel);

        var subtitleLabel = new StyledLabel(
            subtitle,
            _scale,
            fontSize: _compact ? 12 : 13,
            align: HorizontalAlignment.Left
        );
        subtitleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        subtitleLabel.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        heading.AddChild(subtitleLabel);
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
        _homeDestination.Visible = _destination == LauncherDestination.Home;
        _savesDestination.Visible = _destination == LauncherDestination.Saves;
        _versionsDestination.Visible = _destination == LauncherDestination.Versions;
        _modsDestination.Visible = _destination == LauncherDestination.Mods;
        _helpDestination.Visible = _destination == LauncherDestination.Help;
    }
}
