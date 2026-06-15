using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

// Owns launcher view references and UI behavior.
internal sealed partial class LauncherView
{
    private LoginSection Login { get; }
    private CodeSection Code { get; }
    private DownloadSection Download { get; }
    private ActionSection Actions { get; }
    private RichTextLabel Log { get; }

    private readonly Control _parent;
    private readonly StyledPanel _panel;
    private readonly float _panelBaseY;
    private readonly float _scale;
    private readonly LauncherLayoutProfile _profile;
    private readonly StyledLabel _statusPhaseLabel;
    private readonly StyledLabel _statusActionLabel;
    private readonly StyledLabel _statusLabel;
    private readonly ColorRect _statusAccent;

    internal LauncherView(Control parent, LauncherLayoutProfile profile)
    {
        var dismissKeyboard = new Action<InputEvent>(DismissKeyboard);
        var shell = BuildShell(parent, profile, dismissKeyboard);
        _parent = parent;
        _panel = shell.Panel;
        _panelBaseY = shell.Panel.Position.Y;
        _scale = profile.Scale;
        _profile = profile;
        var primary = BuildPrimaryColumn(profile, shell.Content);
        _statusPhaseLabel = primary.StatusPhase;
        _statusActionLabel = primary.StatusAction;
        _statusLabel = primary.Status;
        _statusAccent = primary.StatusAccent;
        Login = primary.Login;
        Code = primary.Code;
        Download = primary.Download;
        Actions = primary.Actions;
        Log = BuildLogColumn(profile, shell.Content, dismissKeyboard);
    }

    internal void SetStatus(string text)
    {
        var phase = LauncherPortalStatusFormatter.PhaseFor(text);
        var color = LauncherPortalStatusFormatter.ColorFor(phase);
        _statusPhaseLabel.Text = phase;
        _statusPhaseLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, color);
        _statusActionLabel.Text = LauncherPortalStatusFormatter.ActionFor(text);
        _statusAccent.Color = color;
        _statusLabel.Text = LauncherPortalStatusFormatter.MessageFor(text);
    }

    internal void AppendLog(string msg) => AppendLogLine(Log, msg);

    internal void AppendColoredLog(string msg, Godot.Color color)
        => AppendColoredLogLine(Log, msg, color);

    internal void HideAllSections()
    {
        Login.Visible = false;
        Code.Visible = false;
        Download.Visible = false;
        Actions.HideAll();
    }
}
