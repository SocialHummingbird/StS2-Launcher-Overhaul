using Godot;

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
        SetCompactActionButtonText(_launchButton, _compact ? CompactLaunchButtonText(launchTitle) : launchTitle);
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: launch buttons");
        ShowLaunchButtons(showUpdate);
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: retry hidden");
        _retryButton.Visible = false;
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase complete");
    }

    internal void ShowRetry()
    {
        Visible = true;
        ApplyDestinationVisibility();
        _retryButton.Visible = true;
        ShowRetryButtons();
    }

    internal void HideAll()
    {
        Visible = true;
        ApplyDestinationVisibility();
        _retryButton.Visible = false;
        HideSecondaryButtons();
    }

    internal Control ReadyScrollTarget => _launchButton;

    internal Control RetryScrollTarget => _retryButton;
}
