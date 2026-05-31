using System;
using System.IO;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

// Owns launcher view references and UI behavior.
internal sealed class LauncherView
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

    internal void UpdateKeyboardOffset()
    {
        var kbHeight = DisplayServer.VirtualKeyboardGetHeight();
        if (kbHeight > 0)
        {
            var windowSize = DisplayServer.WindowGetSize();
            var vpSize = _parent.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            var scale = vpSize.Y / windowSize.Y;
            var offset = kbHeight * scale * 0.5f;
            _panel.Position = new Vector2(_panel.Position.X, _panelBaseY - offset);
            return;
        }

        _panel.Position = new Vector2(_panel.Position.X, _panelBaseY);
    }

    internal void ShowConfirmation(string message, Action onConfirmed)
    {
        var dialog = new StyledDialog(message, _scale);
        dialog.Confirmed += onConfirmed;
        _parent.AddChild(dialog);
    }

    private void DismissKeyboard(InputEvent ev)
    {
        if (ev is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
            _parent.GetViewport()?.GuiReleaseFocus();
    }

    private static (StyledPanel Panel, HBoxContainer RootColumns) BuildShell(
        Control parent,
        float scale,
        Action<InputEvent> dismissKeyboard
    )
    {
        parent.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ScreenBackground();
        background.GuiInput += dismissKeyboard;
        parent.AddChild(background);

        var panel = new StyledPanel(scale, widthRatio: 0.9f);
        panel.UpdateSizeFromViewport(
            parent.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080)
        );
        panel.Panel.GuiInput += dismissKeyboard;
        parent.AddChild(panel);

        var rootColumns = new HBoxContainer();
        rootColumns.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rootColumns.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        rootColumns.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherViewLayoutMetrics.RootColumnSeparation, scale)
        );
        panel.Content.AddChild(rootColumns);

        return (panel, rootColumns);
    }

    private static (
        StyledLabel StatusLabel,
        LoginSection Login,
        CodeSection Code,
        DownloadSection Download,
        ActionSection Actions
    ) BuildPrimaryColumn(float scale, HBoxContainer hbox)
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

        left.AddChild(new FmodAttributionSection(scale));

        return (statusLabel, login, code, download, actions);
    }

    private static LogView BuildLogColumn(
        float scale,
        HBoxContainer hbox,
        Action<InputEvent> dismissKeyboard
    )
    {
        var right = new VBoxContainer();
        right.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        right.SizeFlagsStretchRatio = LauncherViewLayoutMetrics.LogColumnStretchRatio;
        hbox.AddChild(right);

        var logTitle = new StyledLabel(
            "Console",
            scale,
            fontSize: LauncherViewLayoutMetrics.LogTitleFontSize
        );
        logTitle.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        right.AddChild(logTitle);

        var log = new LogView(scale);
        log.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        log.GuiInput += dismissKeyboard;
        right.AddChild(log);
        return log;
    }

    private sealed class LogView : RichTextLabel
    {
        private LogView(float scale)
        {
            CustomMinimumSize = new Vector2(
                0,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogHeight)
            );
            ScrollFollowing = true;
            BbcodeEnabled = true;
            AddThemeFontSizeOverride(
                LauncherComponentTheme.NormalFontSize,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogFontSize)
            );
            AddThemeColorOverride(LauncherComponentTheme.DefaultColor, LauncherComponentTheme.LogText);
            AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, BuildStyle(scale));
        }

        private void AppendLog(string msg) => AddText(msg + "\n");

        private void AppendColoredLog(string msg, Color color)
        {
            PushColor(color);
            AddText(msg + "\n");
            Pop();
        }

        private static StyleBoxFlat BuildStyle(float scale)
        {
            var background = new StyleBoxFlat();
            background.BgColor = LauncherComponentTheme.LogBackground;
            background.SetCornerRadiusAll(
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogRadius)
            );
            background.ContentMarginLeft = LauncherComponentTheme.ScaleInt(
                scale,
                LauncherComponentTheme.LogMarginHorizontal
            );
            background.ContentMarginRight = LauncherComponentTheme.ScaleInt(
                scale,
                LauncherComponentTheme.LogMarginHorizontal
            );
            background.ContentMarginTop = LauncherComponentTheme.ScaleInt(
                scale,
                LauncherComponentTheme.LogMarginVertical
            );
            background.ContentMarginBottom = LauncherComponentTheme.ScaleInt(
                scale,
                LauncherComponentTheme.LogMarginVertical
            );
            return background;
        }
    }

    private sealed class StyledDialog : ColorRect
    {
        private event Action Confirmed;
        private event Action Cancelled;

        private StyledDialog(string message, float scale)
        {
            SetAnchorsPreset(Control.LayoutPreset.FullRect);
            Color = LauncherComponentTheme.DialogOverlay;

            var center = BuildCenter();
            var dialogBox = BuildDialogBox(scale);
            var vbox = BuildContentBox(scale);
            dialogBox.AddChild(vbox);

            vbox.AddChild(BuildMessage(message, scale));
            vbox.AddChild(BuildButtons(scale));

            center.AddChild(dialogBox);
            AddChild(center);
        }

        private static CenterContainer BuildCenter()
        {
            var center = new CenterContainer();
            center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            return center;
        }

        private static PanelContainer BuildDialogBox(float scale)
        {
            var dialogBox = new PanelContainer();
            var boxStyle = new StyleBoxFlat();
            boxStyle.BgColor = LauncherComponentTheme.DialogPanelBackground;
            boxStyle.SetCornerRadiusAll(
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogPanelRadius)
            );
            boxStyle.SetContentMarginAll(
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogPanelMargin)
            );
            dialogBox.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, boxStyle);
            return dialogBox;
        }

        private static VBoxContainer BuildContentBox(float scale)
        {
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride(
                LauncherComponentTheme.ThemeSeparation,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogContentSeparation)
            );
            return vbox;
        }

        private static Label BuildMessage(string message, float scale)
        {
            var label = new StyledLabel(
                message,
                scale,
                fontSize: LauncherComponentTheme.DialogMessageFontSize
            );
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.CustomMinimumSize = new Vector2(
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogMessageWidth),
                0
            );
            label.HorizontalAlignment = HorizontalAlignment.Center;
            return label;
        }

        private HBoxContainer BuildButtons(float scale)
        {
            var buttonRow = new HBoxContainer();
            buttonRow.AddThemeConstantOverride(
                LauncherComponentTheme.ThemeSeparation,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonSeparation)
            );
            buttonRow.Alignment = BoxContainer.AlignmentMode.Center;

            buttonRow.AddChild(BuildButton("Cancel", scale, () => Cancelled?.Invoke()));
            buttonRow.AddChild(BuildButton("OK", scale, () => Confirmed?.Invoke()));

            return buttonRow;
        }

        private Button BuildButton(string text, float scale, Action callback)
        {
            var button = new StyledButton(
                text,
                scale,
                LauncherComponentTheme.DialogButtonFontSize,
                LauncherComponentTheme.DialogButtonHeight
            );
            button.CustomMinimumSize = new Vector2(
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonWidth),
                button.CustomMinimumSize.Y
            );
            button.Pressed += () =>
            {
                QueueFree();
                callback?.Invoke();
            };
            return button;
        }
    }

    private sealed class FmodAttributionSection : VBoxContainer
    {
        private const string CreditText = "Made using FMOD Studio by Firelight Technologies Pty Ltd.";
        private const int CreditFontSize = 8;
        private const string LogoFileName = "fmod_logo.png";
        private const int LogoHeight = 30;
        private const int LogoWidth = 120;
        private static readonly Color CreditColor = new(0.5f, 0.5f, 0.55f);

        private FmodAttributionSection(float scale)
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            Alignment = BoxContainer.AlignmentMode.End;

            var logo = LoadLogo(scale);
            if (logo != null)
                AddChild(logo);

            var credit = new StyledLabel(
                CreditText,
                scale,
                fontSize: CreditFontSize
            );
            credit.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                CreditColor
            );
            AddChild(credit);
        }

        private static TextureRect LoadLogo(float scale)
        {
            try
            {
                var logoPath = Path.Combine(OS.GetDataDir(), LogoFileName);
                if (!File.Exists(logoPath))
                {
                    PatchHelper.Log($"FMOD logo not found at {logoPath}");
                    return null;
                }

                var bytes = File.ReadAllBytes(logoPath);
                var image = new Image();
                image.LoadPngFromBuffer(bytes);
                var texture = ImageTexture.CreateFromImage(image);

                return new TextureRect
                {
                    Texture = texture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    CustomMinimumSize = new Vector2(
                        (int)(LogoWidth * scale),
                        (int)(LogoHeight * scale)
                    ),
                };
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Failed to load FMOD logo: {ex.Message}");
                return null;
            }
        }
    }
}
