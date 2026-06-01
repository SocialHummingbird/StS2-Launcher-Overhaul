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
    private readonly StyledLabel _statusLabel;

    internal LauncherView(Control parent, float scale)
    {
        var dismissKeyboard = new Action<InputEvent>(DismissKeyboard);
        var shell = BuildShell(parent, scale, dismissKeyboard);
        _parent = parent;
        _panel = shell.PanelControl();
        _panelBaseY = shell.PanelBaseY();
        _scale = scale;
        var primary = shell.BuildPrimaryColumn(scale);
        _statusLabel = primary.Status();
        Login = primary.LoginSection();
        Code = primary.CodeSection();
        Download = primary.DownloadSection();
        Actions = primary.ActionSection();
        Log = shell.BuildLogColumn(scale, dismissKeyboard);
    }

    internal void SetStatus(string text) => _statusLabel.Text = text;

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
