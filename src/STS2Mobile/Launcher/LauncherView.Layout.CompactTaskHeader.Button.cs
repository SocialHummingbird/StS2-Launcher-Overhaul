using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static readonly CompactButtonDetailLabelSpec CompactCurrentTaskButtonLabels =
        CompactButtonDetailLabelSpec.Default(
            CompactCurrentTaskButtonBodyName,
            CompactCurrentTaskButtonTitleName,
            CompactCurrentTaskButtonDetailName
        );

    private static Button BuildCompactCurrentTaskButton(float scale, bool compact)
    {
        if (!compact)
            return new Button { Visible = false };

        var button = new StyledButton(
            "",
            scale,
            fontSize: LauncherSectionMetrics.CompactDetailButtonFontSize,
            height: LauncherSectionMetrics.CompactDetailButtonHeight
        );
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        LauncherButtonStyles.ApplySupportAction(button, scale);
        SetCompactCurrentTaskButtonText(button, scale, "Start here", "Setup guide");
        return button;
    }

    private static void SetCompactCurrentTaskButtonText(
        Button button,
        float scale,
        string title,
        string detail
    )
        => CompactButtonDetailLabels.Apply(
            button,
            $"{title}\n{detail}",
            scale,
            enabled: true,
            CompactCurrentTaskButtonLabels
        );
}
