using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private Button BuildRefreshBranchesButton(float scale, bool compact)
    {
        var button = new StyledButton(
            compact ? "" : "Refresh Game Versions",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDetailButtonHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        button.Pressed += () => RefreshGameVersionsRequested?.Invoke();
        if (compact)
        {
            SetCompactVersionActionButtonText(
                button,
                "Refresh Versions",
                "Update branch list"
            );
        }
        return button;
    }

    private void AddRefreshBranchesButtonToLayout()
    {
        if (_compact)
        {
            _compactVersionControlsRow.AddChild(_refreshBranchesButton);
            return;
        }

        AddChild(_refreshBranchesButton);
    }

    private Label BuildBranchHelpLabel(float scale, bool compact)
    {
        var label = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactVersionHelpFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = compact;
        label.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactVersionHelpHeight, scale)
            );
        }
        label.MouseFilter = MouseFilterEnum.Ignore;
        label.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        return label;
    }
}
