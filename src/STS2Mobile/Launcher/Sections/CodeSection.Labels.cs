using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class CodeSection
{
    private static Label CreateCodePromptLabel(float scale, bool compact)
    {
        var label = new StyledLabel(
            DefaultPrompt,
            scale,
            fontSize: compact
                ? CompactCodePromptFontSize
                : LauncherSectionMetrics.PromptFontSize
        );

        if (compact)
        {
            label.AutowrapMode = TextServer.AutowrapMode.Off;
            ConfigureCompactCodeLabel(label, scale, CompactCodePromptHeight);
        }

        return label;
    }

    private static Label CreateCodeHelpLabel(float scale, bool compact)
    {
        var label = new StyledLabel(
            CodeHelpText(compact, wasIncorrect: false),
            scale,
            fontSize: compact
                ? CompactCodeHelpFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: compact
                ? HorizontalAlignment.Center
                : HorizontalAlignment.Left
        );
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        if (compact)
            ConfigureCompactCodeLabel(label, scale, CompactCodeHelpHeight);

        return label;
    }

    private static void ConfigureCompactCodeLabel(Label label, float scale, int height)
    {
        label.ClipText = true;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(height, scale)
        );
    }
}
