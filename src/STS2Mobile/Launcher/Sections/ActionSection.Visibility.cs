using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal void ShowLaunch(string text, bool showUpdate)
    {
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: visible");
        Visible = true;
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: launch button text");
        SetCompactActionButtonText(_launchButton, _compact ? CompactLaunchButtonText(text) : text);
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: launch buttons");
        ShowLaunchButtons(showUpdate);
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: cloud controls");
        SetCloudControlsVisible(true);
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase: retry hidden");
        _retryButton.Visible = false;
        PatchHelper.Log("[Launcher] ActionSection.ShowLaunch phase complete");
    }

    internal void ShowRetry()
    {
        Visible = true;
        _retryButton.Visible = true;
        SetCloudControlsVisible(false);
        ShowRetryButtons();
    }

    internal void HideAll()
    {
        Visible = false;
        _retryButton.Visible = false;
        SetCloudControlsVisible(false);
        HideSecondaryButtons();
    }

    internal Control ReadyScrollTarget => _compact ? _cloudGroup : _launchButton;

    internal Control RetryScrollTarget => _retryButton;
}
