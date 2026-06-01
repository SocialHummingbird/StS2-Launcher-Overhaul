using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private readonly struct PrimaryColumnControls
    {
        private PrimaryColumnControls(
            StyledLabel statusLabel,
            LoginSection login,
            CodeSection code,
            DownloadSection download,
            ActionSection actions
        )
        {
            StatusLabel = statusLabel;
            Login = login;
            Code = code;
            Download = download;
            Actions = actions;
        }

        private StyledLabel StatusLabel { get; }
        private LoginSection Login { get; }
        private CodeSection Code { get; }
        private DownloadSection Download { get; }
        private ActionSection Actions { get; }

        internal static PrimaryColumnControls Create(
            StyledLabel statusLabel,
            LoginSection login,
            CodeSection code,
            DownloadSection download,
            ActionSection actions
        )
            => new(statusLabel, login, code, download, actions);

        internal StyledLabel Status()
            => StatusLabel;

        internal LoginSection LoginSection()
            => Login;

        internal CodeSection CodeSection()
            => Code;

        internal DownloadSection DownloadSection()
            => Download;

        internal ActionSection ActionSection()
            => Actions;
    }

    private static PrimaryColumnControls BuildPrimaryColumn(float scale, HBoxContainer hbox)
    {
        var leftCenter = new CenterContainer();
        leftCenter.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftCenter.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftCenter.SizeFlagsStretchRatio = LauncherViewLayoutMetrics.PrimaryColumnStretchRatio;
        hbox.AddChild(leftCenter);

        var left = new VBoxContainer();
        left.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(LauncherViewLayoutMetrics.PrimaryColumnMinWidth, scale),
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

        return PrimaryColumnControls.Create(statusLabel, login, code, download, actions);
    }
}
