using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal void SetCloudOperationState(CloudOperationState state)
    {
        var presentation = CloudOperationPresentation.Create(state);
        _cloudOperationProgressGroup.Visible = presentation.Visible;
        _cloudOperationPhaseLabel.Text = presentation.PhaseText;
        _cloudOperationDetailLabel.Text = presentation.DetailText;
        _cloudOperationProgressBar.Value = presentation.ProgressValue;
        _cloudOperationProgressBar.TooltipText =
            $"{presentation.PhaseText}\n{presentation.DetailText}";
        _cloudOperationPhaseLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            presentation.IsFailed
                ? LauncherComponentTheme.OrangeHot
                : presentation.IsPartial
                    ? LauncherComponentTheme.OrangeHot
                : presentation.IsComplete
                    ? LauncherComponentTheme.TextPrimary
                    : LauncherComponentTheme.CyanAccent
        );
    }

    internal void ClearCloudOperationState()
    {
        _cloudOperationProgressGroup.Visible = false;
        _cloudOperationPhaseLabel.Text = "";
        _cloudOperationDetailLabel.Text = "";
        _cloudOperationProgressBar.Value = 0;
        _cloudOperationProgressBar.TooltipText = "";
    }

    private void RequestCloudOperationCancellation()
    {
        _cancelCloudOperationButton.Disabled = true;
        SetCompactActionButtonText(
            _cancelCloudOperationButton,
            "Cancelling..."
        );
        CloudOperationCancelPressed?.Invoke();
    }
}
