using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private Button AddPrimaryHiddenButton(Container parent, string text, float scale, Action pressed)
        => AddHiddenButton(
            parent,
            text,
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            pressed
        );

    private Button AddSecondaryHiddenButton(Container parent, string text, float scale, Action pressed)
        => AddHiddenButton(
            parent,
            text,
            scale,
            LauncherSectionMetrics.SecondaryButtonFontSize,
            LauncherSectionMetrics.SecondaryButtonHeight,
            pressed
        );

    private Button AddHiddenButton(
        Container parent,
        string text,
        float scale,
        int fontSize,
        int height,
        Action pressed
    )
    {
        var button = new StyledButton(text, scale, fontSize: fontSize, height: height);
        button.Visible = false;
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (pressed != null)
            button.Pressed += pressed;
        parent.AddChild(button);
        return button;
    }

    private static Button AddPushPullButton(
        Container row,
        string text,
        float scale,
        Action pressed
    )
    {
        var button = new StyledButton(
            text,
            scale,
            LauncherSectionMetrics.SecondaryButtonFontSize,
            LauncherSectionMetrics.SecondaryButtonHeight
        );
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (pressed != null)
            button.Pressed += pressed;
        row.AddChild(button);
        return button;
    }
}
