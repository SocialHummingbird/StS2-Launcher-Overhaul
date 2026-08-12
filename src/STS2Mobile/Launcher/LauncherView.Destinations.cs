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
        DiagnosticsToggle.Visible = destination == LauncherDestination.Help;
        if (destination != LauncherDestination.Help)
            DiagnosticsDrawer.Visible = false;

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
        _androidCompositionRefresh.Color = _androidCompositionRefreshFrames == 0
            ? Colors.Transparent
            : (frame & 1) == 0
                ? AndroidCompositionRefreshTintA
                : AndroidCompositionRefreshTintB;
    }

    private void SelectHomeDestination()
    {
        if (_destination != LauncherDestination.Home)
            SelectDestination(LauncherDestination.Home);
    }

    private void ApplyDestinationButtonStyle(Button button, bool selected)
    {
        LauncherButtonStyles.ApplySupportAction(button, _scale);
        if (selected)
        {
            button.AddThemeStyleboxOverride(
                "normal",
                LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.ButtonHover, LauncherComponentTheme.ScaleInt(_scale, 6))
            );
        }
        button.AddThemeColorOverride(
            "font_color",
            selected ? LauncherComponentTheme.CyanAccent : LauncherComponentTheme.TextSecondary
        );
        button.AddThemeColorOverride(
            "font_hover_color",
            selected ? LauncherComponentTheme.CyanAccent : LauncherComponentTheme.TextPrimary
        );
        button.AddThemeColorOverride(
            "font_pressed_color",
            LauncherComponentTheme.CyanAccent
        );
    }
}
