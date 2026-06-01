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
    private LogView Log { get; }

    private readonly Control _parent;
    private readonly StyledPanel _panel;
    private readonly float _panelBaseY;
    private readonly float _scale;
    private readonly StyledLabel _statusLabel;

    internal LauncherView(Control parent, float scale)
    {
        var dismissKeyboard = new Action<InputEvent>(DismissKeyboard);
        var (panel, rootColumns) = BuildShell(parent, scale, dismissKeyboard);
        _parent = parent;
        _panel = panel;
        _panelBaseY = panel.Position.Y;
        _scale = scale;
        (_statusLabel, Login, Code, Download, Actions) = BuildPrimaryColumn(
            scale,
            rootColumns
        );
        Log = BuildLogColumn(scale, rootColumns, dismissKeyboard);
    }

    internal void SetStatus(string text) => _statusLabel.Text = text;

    internal void AppendLog(string msg) => Log.AppendLog(msg);

    internal void AppendColoredLog(string msg, Godot.Color color) => Log.AppendColoredLog(msg, color);

    internal void HideAllSections()
    {
        Login.Visible = false;
        Code.Visible = false;
        Download.Visible = false;
        Actions.HideAll();
    }
}
