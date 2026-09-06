using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal event Action<LauncherDestination> HomeDestinationPressed;

    private Control BuildHomeShortcuts()
    {
        var group = new VBoxContainer { Name = "HomeShortcuts" };
        group.AddThemeConstantOverride("separation", LauncherViewLayoutMetrics.ScaleInt(10, _scale));
        var label = new StyledLabel("YOUR GAME", _scale, fontSize: 12, align: HorizontalAlignment.Left);
        label.AddThemeColorOverride("font_color", LauncherComponentTheme.TextSecondary);
        group.AddChild(new Control { CustomMinimumSize = new Vector2(0, 12 * _scale), MouseFilter = Control.MouseFilterEnum.Ignore });
        group.AddChild(label);
        AddHomeShortcut(group, LauncherDestination.Saves, "Manage saves", "Sync progress with Steam");
        AddHomeShortcut(group, LauncherDestination.Versions, "Game version", "Choose, update, or repair your download");
        AddHomeShortcut(group, LauncherDestination.Mods, "Mods & play style", "Switch between Vanilla and Modded");
        return group;
    }

    private void AddHomeShortcut(VBoxContainer group, LauncherDestination destination, string title, string detail)
    {
        var button = new StyledButton("", _scale, height: 56)
        {
            Name = $"HomeShortcut{destination}",
            AccessibilityName = title,
            AccessibilityDescription = detail,
            TooltipText = detail,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        LauncherButtonStyles.ApplySupportAction(button, _scale);
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        row.OffsetLeft = 16 * _scale;
        row.OffsetRight = -16 * _scale;
        row.AddThemeConstantOverride("separation", LauncherViewLayoutMetrics.ScaleInt(14, _scale));
        row.AddChild(new TextureRect
        {
            Texture = LauncherIcons.Create((int)destination, _scale),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            Modulate = LauncherComponentTheme.CyanAccent,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        var copy = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        foreach (var item in new[] { (title, 16, LauncherComponentTheme.TextPrimary), (detail, 12, LauncherComponentTheme.TextSecondary) })
        {
            var line = new StyledLabel(item.Item1, _scale, fontSize: item.Item2, align: HorizontalAlignment.Left)
            {
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            line.AddThemeColorOverride("font_color", item.Item3);
            copy.AddChild(line);
        }
        row.AddChild(copy);
        row.AddChild(new StyledLabel("›", _scale, fontSize: 24) { MouseFilter = Control.MouseFilterEnum.Ignore });
        button.AddChild(row);
        button.Pressed += () => HomeDestinationPressed?.Invoke(destination);
        group.AddChild(button);
    }
}


