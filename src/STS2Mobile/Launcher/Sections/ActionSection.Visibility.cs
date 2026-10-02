using Godot;
using System;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class ActionSection
{
    internal void ShowLaunch(string text, bool showUpdate)
    {
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: visible");
        Visible = true;
        ApplyDestinationVisibility();
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: launch button text");
        var launchTitle = LaunchTitle(text);
        if (string.Equals(launchTitle, "Play", System.StringComparison.OrdinalIgnoreCase))
            ApplyContextualPlayLabel();
        else
            SetCompactActionButtonText(_launchButton, _compact ? CompactLaunchButtonText(launchTitle) : launchTitle);
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: launch buttons");
        ShowLaunchButtons(showUpdate);
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: retry hidden");
        _retryButton.Visible = false;
        _homeHelpButton.Visible = false;
        ApplyDestinationVisibility();
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase complete");
    }

    internal void ShowRetry()
    {
        Visible = true;
        ApplyDestinationVisibility();
        _retryButton.Visible = true;
        _homeHelpButton.Visible = false;
        ShowRetryButtons();
        ApplyDestinationVisibility();
    }

    internal void HideAll()
    {
        Visible = true;
        ApplyDestinationVisibility();
        _retryButton.Visible = false;
        _homeHelpButton.Visible = false;
        HideSecondaryButtons();
        ApplyDestinationVisibility();
    }

    internal Control ReadyScrollTarget => _launchButton;
    internal Control RetryScrollTarget => _retryButton;

    private static string CompactLaunchButtonText(string text) => LaunchTitle(text);
    private static string LaunchTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Play";
        var normalized = text.Trim();
        return string.Equals(normalized, "Start Game", StringComparison.OrdinalIgnoreCase) ? "Play" : normalized;
    }

    private static string CompactPlaySyncDrawerText(string action, string detail) => $"{action}\n{detail}";
    private void ShowLaunchButtons(bool showUpdate) => SetSecondaryButtonsVisible(SecondaryButtonVisibility.LaunchReady(showUpdate));
    private void ShowRetryButtons() => SetSecondaryButtonsVisible(SecondaryButtonVisibility.Retry());
    private void HideSecondaryButtons() => SetSecondaryButtonsVisible(SecondaryButtonVisibility.Hidden());
    private void SetSecondaryButtonsVisible(SecondaryButtonVisibility visibility)
    {
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: update");
        ShowUpdateButton(visibility.Update);
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: redownload");
        _redownloadButton.Visible = visibility.Redownload;
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: mods");
        ShowModsControls();
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: support");
        ShowDestinationTools();
        UpdateVersionMaintenanceVisibility();
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: safe launch");
        _safeLaunchButton.Visible = visibility.SafeLaunch;
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: launch");
        _launchButton.Visible = visibility.Launch;
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase: disabled state");
        ApplyLaunchControlsDisabled();
        PatchHelper.Log("[Launcher] ActionSection secondary visibility phase complete");
    }

    private readonly struct SecondaryButtonVisibility
    {
        private SecondaryButtonVisibility(bool update, bool redownload, bool safeLaunch, bool launch)
        {
            Update = update;
            Redownload = redownload;
            SafeLaunch = safeLaunch;
            Launch = launch;
        }

        internal bool Update { get; }
        internal bool Redownload { get; }
        internal bool SafeLaunch { get; }
        internal bool Launch { get; }

        internal static SecondaryButtonVisibility LaunchReady(bool showUpdate) => new(update: showUpdate, redownload: true, safeLaunch: true, launch: true);
        internal static SecondaryButtonVisibility Retry() => new(update: false, redownload: false, safeLaunch: false, launch: false);
        internal static SecondaryButtonVisibility Hidden() => new(update: false, redownload: false, safeLaunch: false, launch: false);
    }

    private void ShowUpdateButton(bool visible)
    {
        _updateButton.Visible = visible;
        _updateButton.Disabled = false;
        SetVersionPrimaryAction(LauncherVersionPrimaryAction.CheckForUpdates);
    }

    private void ShowDestinationTools()
    {
        _diagnosticsButton.Visible = true;
        _refreshVersionsButton.Visible = true;
        _redownloadButton.Visible = true;
        _showLastErrorButton.Visible = true;
        _copyRawLogButton.Visible = true;
    }
}
