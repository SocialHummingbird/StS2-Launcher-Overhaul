using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private void SetCompactWorkflowStep(CompactWorkflowStep step)
    {
        if (!_profile.Compact || _workflowStepLabels.Length == 0)
            return;

        var activeIndex = (int)step;
        for (var i = 0; i < _workflowStepLabels.Length; i++)
        {
            var active = i == activeIndex;
            var complete = i < activeIndex;
            var color = active
                ? LauncherComponentTheme.OrangeHot
                : complete
                    ? LauncherComponentTheme.CyanAccent
                    : LauncherComponentTheme.TextMuted;
            _workflowStepLabels[i].AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                color
            );
            if (i < _workflowStepNumberLabels.Length)
            {
                _workflowStepNumberLabels[i].AddThemeColorOverride(
                    LauncherViewLayoutMetrics.ThemeFontColor,
                    color
                );
            }
            if (i < _workflowStepDetailLabels.Length)
            {
                _workflowStepDetailLabels[i].AddThemeColorOverride(
                    LauncherViewLayoutMetrics.ThemeFontColor,
                    active
                        ? LauncherComponentTheme.TextSecondary
                        : complete
                            ? LauncherComponentTheme.CyanDim
                            : LauncherComponentTheme.TextMuted
                );
            }
            _workflowStepAccents[i].Color = active
                ? LauncherComponentTheme.OrangeAccent
                : complete
                    ? LauncherComponentTheme.CyanDim
                    : LauncherComponentTheme.ButtonNormal;
        }
    }

    private void SetCompactCurrentTask(string text, Control target, string detail)
    {
        if (!_profile.Compact)
            return;

        SetCompactCurrentTaskButtonText(_compactCurrentTaskButton, _scale, text, detail);
        _compactCurrentTaskButton.Visible = true;
        _compactCurrentTaskTarget = target;
        _compactScrollAnchorTarget = target;
    }
}
