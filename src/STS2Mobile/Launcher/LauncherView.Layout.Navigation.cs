using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private readonly record struct DestinationNavigation(MarginContainer Root, Button[] Buttons);

    private static DestinationNavigation BuildDestinationNavigation(LauncherLayoutProfile profile)
    {
        var frame = new MarginContainer
        {
            Name = "DestinationNavigation",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var navigation = new GridContainer
        {
            Columns = 5,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        navigation.AddThemeConstantOverride(
            "h_separation",
            LauncherViewLayoutMetrics.ScaleInt(profile.Compact ? 3 : 6, profile.Scale)
        );
        frame.AddChild(navigation);

        string[] labels = ["Home", "Saves", "Versions", "Mods", "Help"];
        var buttons = new Button[labels.Length];
        for (var i = 0; i < labels.Length; i++)
        {
            var button = new StyledButton(
                labels[i],
                profile.Scale,
                fontSize: profile.Compact ? 12 : 14,
                height: 66
            )
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                FocusMode = Control.FocusModeEnum.All,
                ToggleMode = true,
                Icon = LauncherIcons.Create(i, profile.Scale * (profile.Compact ? 0.83f : 0.75f)),
                IconAlignment = HorizontalAlignment.Center,
                VerticalIconAlignment = VerticalAlignment.Top,
                TooltipText = labels[i],
            };
            LauncherButtonStyles.ApplySupportAction(button, profile.Scale);
            navigation.AddChild(button);
            buttons[i] = button;
        }

        return new DestinationNavigation(frame, buttons);
    }

    internal void UpdateSystemInsets()
    {
        if (!_profile.Compact || !OperatingSystem.IsAndroid())
            return;

        var safeArea = DisplayServer.GetDisplaySafeArea();
        if (safeArea.Size.X <= 0 || safeArea.Size.Y <= 0)
            return;

        var viewportSize = _parent.GetViewport()?.GetVisibleRect().Size
            ?? _profile.ViewportSize;
        var obscuredLeft = Math.Max(0, (int)MathF.Ceiling(safeArea.Position.X));
        var obscuredTop = Math.Max(0, (int)MathF.Ceiling(safeArea.Position.Y));
        var obscuredRight = Math.Max(
            0,
            (int)MathF.Ceiling(viewportSize.X - safeArea.Position.X - safeArea.Size.X)
        );
        var obscuredBottom = Math.Max(
            0,
            (int)MathF.Ceiling(
                viewportSize.Y - safeArea.Position.Y - safeArea.Size.Y
            )
        );
        var insets = new Vector4(obscuredLeft, obscuredTop, obscuredRight, obscuredBottom);
        if (insets == _systemSafeAreaInsets)
            return;

        _systemSafeAreaInsets = insets;
        _panel.UpdateSafeAreaContentInsets(obscuredLeft, obscuredTop, obscuredRight);
        var bottomInset = obscuredBottom
            + LauncherViewLayoutMetrics.ScaleInt(4, _profile.Scale);
        _destinationSafeAreaSpacer.CustomMinimumSize = new Vector2(0, bottomInset);
        RequestAndroidCompositionRefresh();
        GD.Print(
            $"Launcher safe area: viewport={viewportSize} safe={safeArea} "
            + $"insets=({obscuredLeft},{obscuredTop},{obscuredRight},{obscuredBottom})"
        );
    }
}
