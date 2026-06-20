using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class CodeSection
{
    private static readonly CompactButtonDetailLabelSpec CompactCodeSubmitLabels =
        CompactButtonDetailLabelSpec.Default(
            "CompactCodeSubmitBody",
            "CompactCodeSubmitTitle",
            "CompactCodeSubmitDetail"
        );

    private static string CompactCodeSubmitText()
        => "Verify Code\nSubmit once";

    private static Button CreateCodeSubmitButton(float scale, bool compact)
    {
        var button = new StyledButton(
            compact ? CompactCodeSubmitText() : "Verify Code",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CodeSubmitFontSize
                : LauncherSectionMetrics.PrimaryButtonFontSize,
            height: CodeInputHeight(compact)
        );
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        LauncherButtonStyles.ApplyPrimaryAction(button, scale);
        SetCompactCodeSubmitButtonText(button, button.Text, scale, compact);
        return button;
    }

    private static void SetCompactCodeSubmitButtonText(
        Button button,
        string text,
        float scale,
        bool compact
    )
        => CompactButtonDetailLabels.Apply(
            button,
            text,
            scale,
            compact,
            CompactCodeSubmitLabels
        );
}
