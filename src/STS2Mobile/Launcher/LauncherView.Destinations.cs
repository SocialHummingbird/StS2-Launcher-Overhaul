using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;
internal sealed partial class LauncherView
{
    private const int AndroidCompositionRefreshFrameCount = 7;
    private static readonly Color AndroidCompositionRefreshTintA = new(0, 0, 0, 1f / 255f);
    private static readonly Color AndroidCompositionRefreshTintB = new(0, 0, 0, 2f / 255f);
    internal event Action<LauncherDestination> DestinationSelected;
    private void WireDestinationNavigation()
    {
        for (var i = 0; i < _destinationButtons.Length; i++)
        {
            var destination = (LauncherDestination)i;
            _destinationButtons[i].Pressed += () => SelectDestination(destination);
        }
    }

    internal void SelectDestination(LauncherDestination destination)
    {
        _destination = destination;
        HomeSections.Visible = destination == LauncherDestination.Home;
        Actions.SetDestination(destination);
        UpdateStatusVisibility();
        DiagnosticsToggle.Visible = destination == LauncherDestination.Help;
        if (destination != LauncherDestination.Help)
        {
            DiagnosticsDrawer.Visible = false;
            SetDiagnosticsToggleText(DiagnosticsToggle, _profile, visible: false);
        }

        for (var i = 0; i < _destinationButtons.Length; i++)
        {
            var selected = i == (int)destination;
            ApplyDestinationButtonStyle(_destinationButtons[i], selected);
            _destinationButtons[i].ButtonPressed = selected;
        }

        PrimaryScroll.ScrollHorizontal = 0;
        PrimaryScroll.ScrollVertical = 0;
        PrimaryScroll.QueueSort();
        Callable.From(ResetDestinationScroll).CallDeferred();
        RequestAndroidCompositionRefresh();
        DestinationSelected?.Invoke(destination);
    }

    private void ResetDestinationScroll()
    {
        PrimaryScroll.ScrollHorizontal = 0;
        PrimaryScroll.ScrollVertical = 0;
        PrimaryScroll.QueueSort();
        PrimaryScroll.QueueRedraw();
    }

    internal void RequestAndroidCompositionRefresh()
    {
        if (OperatingSystem.IsAndroid())
            _androidCompositionRefreshFrames = AndroidCompositionRefreshFrameCount;
    }

    internal void AdvanceAndroidCompositionRefresh()
    {
        if (_androidCompositionRefreshFrames <= 0)
            return;
        // Qualcomm/Godot partial-damage paths can otherwise retain stale launcher regions.
        var frame = _androidCompositionRefreshFrames--;
        _androidCompositionRefresh.Color = _androidCompositionRefreshFrames == 0 ? Colors.Transparent : (frame & 1) == 0 ? AndroidCompositionRefreshTintA : AndroidCompositionRefreshTintB;
    }

    private void SelectHomeDestination()
    {
        if (_destination != LauncherDestination.Home)
            SelectDestination(LauncherDestination.Home);
    }

    private void ApplyDestinationButtonStyle(Button button, bool selected)
    {
        LauncherButtonStyles.ApplySupportAction(button, _scale);
        var navigationStyle = LauncherStyleBoxes.MakeFilled(selected ? LauncherComponentTheme.ButtonHover : LauncherComponentTheme.PanelBackground, LauncherComponentTheme.ScaleInt(_scale, 10));
        navigationStyle.BorderColor = selected ? LauncherComponentTheme.OrangeAccent : LauncherComponentTheme.PanelBackground;
        navigationStyle.BorderWidthBottom = selected ? LauncherComponentTheme.ScaleInt(_scale, 3) : 0;
        button.AddThemeStyleboxOverride("normal", navigationStyle);
        button.AddThemeStyleboxOverride("pressed", navigationStyle);
        button.AddThemeStyleboxOverride("hover_pressed", navigationStyle);
        button.AddThemeColorOverride("icon_normal_color", selected ? LauncherComponentTheme.OrangeAccent : LauncherComponentTheme.TextSecondary);
        button.AddThemeColorOverride("icon_pressed_color", LauncherComponentTheme.OrangeAccent);
        if (selected)
        {
            button.AddThemeStyleboxOverride("normal", navigationStyle);
        }

        button.AddThemeColorOverride("font_color", selected ? LauncherComponentTheme.OrangeAccent : LauncherComponentTheme.TextSecondary);
        button.AddThemeColorOverride("font_hover_color", selected ? LauncherComponentTheme.CyanAccent : LauncherComponentTheme.TextPrimary);
        button.AddThemeColorOverride("font_pressed_color", LauncherComponentTheme.OrangeAccent);
    }

    private static readonly string[] CompactWorkflowStepNames =
    {
        "Sign in",
        "Verify",
        "Files",
        "Play",
    };
    private static readonly string[] CompactWorkflowStepNumbers =
    {
        "1",
        "2",
        "3",
        "4",
    };
    private static readonly string[] CompactWorkflowStepDetails =
    {
        "Account",
        "Steam Guard",
        "Game files",
        "Launch",
    };
    private static readonly string[] CompactWorkflowStepTooltips =
    {
        "Open sign-in",
        "Open Steam Guard",
        "Open game files",
        "Open play controls",
    };
    private enum CompactWorkflowStep
    {
        SignIn = 0,
        Code = 1,
        Files = 2,
        Play = 3,
    }

    private void WireCompactWorkflowStepNavigation()
    {
        if (!_profile.Compact || _workflowStepButtons.Length == 0)
            return;
        for (var i = 0; i < _workflowStepButtons.Length; i++)
        {
            var capturedStep = (CompactWorkflowStep)i;
            _workflowStepButtons[i].Pressed += () => ScrollCompactWorkflowStep(capturedStep);
        }
    }

    private void WireCompactCurrentTaskNavigation()
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(_compactCurrentTaskButton))
            return;
        _compactCurrentTaskButton.Pressed += () => ScrollCompactPrimaryTo(_compactCurrentTaskTarget);
    }

    private void ScrollCompactWorkflowStep(CompactWorkflowStep step)
    {
        if (!_profile.Compact)
            return;
        var target = step switch
        {
            CompactWorkflowStep.SignIn => Login.Visible ? Login : (FirstRunGuide.Visible ? FirstRunGuide : _compactCurrentTaskTarget),
            CompactWorkflowStep.Code => Code.Visible ? Code : _compactCurrentTaskTarget,
            CompactWorkflowStep.Files => Download.Visible ? Download : _compactCurrentTaskTarget,
            CompactWorkflowStep.Play => _compactCurrentTaskTarget,
            _ => _compactCurrentTaskTarget,
        };
        ScrollCompactPrimaryTo(target);
    }

    private void SetCompactWorkflowStep(CompactWorkflowStep step)
    {
        if (!_profile.Compact || _workflowStepLabels.Length == 0)
            return;
        var activeIndex = (int)step;
        for (var i = 0; i < _workflowStepLabels.Length; i++)
        {
            var active = i == activeIndex;
            var complete = i < activeIndex;
            var color = active ? LauncherComponentTheme.OrangeHot : complete ? LauncherComponentTheme.CyanAccent : LauncherComponentTheme.TextMuted;
            _workflowStepLabels[i].AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
            if (i < _workflowStepNumberLabels.Length)
            {
                _workflowStepNumberLabels[i].AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
            }

            if (i < _workflowStepDetailLabels.Length)
            {
                _workflowStepDetailLabels[i].AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, active ? LauncherComponentTheme.TextSecondary : complete ? LauncherComponentTheme.CyanDim : LauncherComponentTheme.TextMuted);
            }

            _workflowStepAccents[i].Color = active ? LauncherComponentTheme.OrangeAccent : complete ? LauncherComponentTheme.CyanDim : LauncherComponentTheme.ButtonNormal;
        }
    }

    private void SetCompactCurrentTask(string text, Control target, string detail)
    {
        if (!_profile.Compact)
            return;
        if (GodotObject.IsInstanceValid(_compactCurrentTaskButton))
        {
            SetCompactCurrentTaskButtonText(_compactCurrentTaskButton, _scale, text, detail);
            _compactCurrentTaskButton.Visible = true;
        }

        _compactCurrentTaskTarget = target;
        _compactScrollAnchorTarget = target;
    }
}
