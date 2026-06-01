using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

// Owns launcher view references and UI behavior.
internal sealed partial class LauncherView
{
    internal LoginSection Login { get; }
    internal CodeSection Code { get; }
    internal DownloadSection Download { get; }
    internal ActionSection Actions { get; }
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
        _panel = shell.Panel;
        _panelBaseY = shell.Panel.Position.Y;
        _scale = scale;
        var primary = BuildPrimaryColumn(scale, shell.RootColumns);
        _statusLabel = primary.StatusLabel;
        Login = primary.Login;
        Code = primary.Code;
        Download = primary.Download;
        Actions = primary.Actions;
        Log = BuildLogColumn(scale, shell.RootColumns, dismissKeyboard);
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
