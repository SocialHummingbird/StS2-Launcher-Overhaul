using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static (
        StyledLabel Status,
        LoginSection Login,
        CodeSection Code,
        DownloadSection Download,
        ActionSection Actions
    ) BuildPrimaryColumn(float scale, HBoxContainer hbox)
    {
        var leftScroll = new ScrollContainer();
        leftScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.SizeFlagsStretchRatio = LauncherViewLayoutMetrics.PrimaryColumnStretchRatio;
        leftScroll.FollowFocus = true;
        hbox.AddChild(leftScroll);

        var leftCenter = new CenterContainer();
        leftCenter.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftCenter.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.AddChild(leftCenter);

        var left = new VBoxContainer();
        left.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.PrimaryColumnMinWidth,
            0
        );
        left.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherViewLayoutMetrics.PrimaryColumnSeparation, scale)
        );
        leftCenter.AddChild(left);

        var title = new StyledLabel("StS2 Launcher", scale, fontSize: 26);
        left.AddChild(title);
        left.AddChild(new HSeparator());

        var statusLabel = new StyledLabel("Initializing...", scale);
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        left.AddChild(statusLabel);

        var login = new LoginSection(scale);
        left.AddChild(login);

        var code = new CodeSection(scale);
        left.AddChild(code);

        var download = new DownloadSection(scale);
        left.AddChild(download);

        var actions = new ActionSection(scale);
        left.AddChild(actions);

        left.AddChild(BuildFmodAttributionSection(scale));

        return (statusLabel, login, code, download, actions);
    }
}
