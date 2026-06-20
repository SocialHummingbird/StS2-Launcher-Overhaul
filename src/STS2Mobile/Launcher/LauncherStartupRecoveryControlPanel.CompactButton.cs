using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const string CompactRecoveryButtonBodyName = "CompactRecoveryButtonBody";
    private const string CompactRecoveryButtonTitleName = "CompactRecoveryButtonTitle";
    private const string CompactRecoveryButtonDetailName = "CompactRecoveryButtonDetail";
    private const int CompactRecoveryButtonTitleFontSize = 16;
    private const int CompactRecoveryButtonDetailFontSize = 12;
    private const int CompactRecoveryButtonHorizontalMargin = 8;
    private const int CompactRecoveryButtonVerticalMargin = 6;

    private static void AddCompactRecoveryButtonLabels(
        Button button,
        float scale,
        string titleText,
        string detailText
    )
    {
        var body = new VBoxContainer
        {
            Name = CompactRecoveryButtonBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonHorizontalMargin);
        body.OffsetRight = -LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonHorizontalMargin);
        body.OffsetTop = LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonVerticalMargin);
        body.OffsetBottom = -LauncherComponentTheme.ScaleInt(scale, CompactRecoveryButtonVerticalMargin);
        body.AddThemeConstantOverride(ThemeSeparation, 0);

        var title = CreateStructuredButtonLabel(
            CompactRecoveryButtonTitleName,
            titleText,
            scale,
            CompactRecoveryButtonTitleFontSize,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detail = CreateStructuredButtonLabel(
            CompactRecoveryButtonDetailName,
            detailText,
            scale,
            CompactRecoveryButtonDetailFontSize,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detail);

        button.AddChild(body);
    }

    private static Label CreateStructuredButtonLabel(
        string name,
        string text,
        float scale,
        int fontSize,
        Color color
    )
    {
        var label = new StyledLabel(
            text,
            scale,
            fontSize: fontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        label.AddThemeColorOverride(ThemeFontColor, color);
        return label;
    }
}
