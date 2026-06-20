using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class CodeSection
{
    private static LineEdit CreateCodeField(float scale, bool compact)
    {
        var field = new StyledLineEdit(
            compact ? "ABC123" : "Steam Guard code",
            scale,
            keyboardType: DisplayServer.VirtualKeyboardType.Default
        );
        field.MaxLength = LauncherSectionMetrics.CodeMaxLength;
        field.Alignment = HorizontalAlignment.Center;
        field.CustomMinimumSize = new Vector2(
            0,
            LauncherComponentTheme.ScaleInt(scale, CodeInputHeight(compact))
        );
        field.AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, CodeInputFontSize(compact))
        );
        field.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return field;
    }

    private static int CodeInputHeight(bool compact)
        => compact
            ? LauncherSectionMetrics.CodeInputHeight
            : LauncherSectionMetrics.PrimaryButtonHeight;

    private static int CodeInputFontSize(bool compact)
        => compact
            ? LauncherSectionMetrics.CodeInputFontSize
            : LauncherSectionMetrics.PrimaryButtonFontSize;
}
