using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly record struct CloudOperationProgressControls(
        VBoxContainer Group,
        Label PhaseLabel,
        Label DetailLabel,
        ProgressBar ProgressBar
    );

    private static CloudOperationProgressControls BuildCloudOperationProgressControls(
        Container parent,
        float scale,
        bool compact
    )
    {
        var group = new VBoxContainer
        {
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        group.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? 4 : 6,
                scale
            )
        );

        var phaseLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactCloudSafetyDetailFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        phaseLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        phaseLabel.ClipText = false;
        phaseLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        group.AddChild(phaseLabel);

        var detailLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactCloudSafetyDetailFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        detailLabel.ClipText = false;
        detailLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(detailLabel);

        var progressBar = new StyledProgressBar(scale, compact)
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        group.AddChild(progressBar);
        parent.AddChild(group);

        return new CloudOperationProgressControls(
            group,
            phaseLabel,
            detailLabel,
            progressBar
        );
    }
}
