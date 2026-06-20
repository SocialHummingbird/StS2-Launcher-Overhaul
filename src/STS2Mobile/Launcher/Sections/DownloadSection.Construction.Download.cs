using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private Button BuildDownloadButton(float scale, bool compact)
    {
        var button = new StyledButton(
            CompactDownloadButtonText(DefaultDownloadButtonText, compact),
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.PrimaryButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            height: compact
                ? CompactDownloadActionHeight
                : LauncherSectionMetrics.DownloadButtonHeight
        );
        SetCompactDownloadButtonText(button, button.Text);
        button.Pressed += () => DownloadRequested?.Invoke();
        return button;
    }

    private static ProgressBar BuildProgressBar(float scale, bool compact)
    {
        var progress = new StyledProgressBar(scale, compact);
        progress.Visible = false;
        return progress;
    }

    private Label BuildProgressLabel(float scale, bool compact)
    {
        var label = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.SecondaryButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize
        );
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = compact;
        label.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactDownloadProgressLabelHeight, scale)
            );
        }
        label.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            compact
                ? LauncherComponentTheme.CyanAccent
                : LauncherViewLayoutMetrics.LogTitleColor
        );
        label.Visible = false;
        return label;
    }
}
