using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal void ShowLaunch(string text, bool showUpdate)
    {
        Visible = true;
        SetCompactActionButtonText(_launchButton, _compact ? CompactLaunchButtonText(text) : text);
        SetCloudControlsVisible(true);
        ShowLaunchButtons(showUpdate);
        _retryButton.Visible = false;
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
