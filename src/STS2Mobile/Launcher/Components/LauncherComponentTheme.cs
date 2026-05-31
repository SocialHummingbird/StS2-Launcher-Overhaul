using Godot;

namespace STS2Mobile.Launcher.Components;

internal static class LauncherComponentTheme
{
    internal const int ButtonDefaultFontSize = 14;
    internal const int ButtonDefaultHeight = 42;
    internal const int ButtonRadius = 4;
    internal const int DialogButtonFontSize = 14;
    internal const int DialogButtonHeight = 44;
    internal const int DialogButtonWidth = 120;
    internal const int DialogButtonSeparation = 12;
    internal const int DialogContentSeparation = 16;
    internal const int DialogMessageFontSize = 16;
    internal const int DialogMessageWidth = 300;
    internal const int DialogPanelMargin = 24;
    internal const int DialogPanelRadius = 8;
    internal const int LineEditFontSize = 14;
    internal const int LineEditHeight = 38;
    internal const int LogFontSize = 11;
    internal const int LogHeight = 120;
    internal const int LogMarginHorizontal = 8;
    internal const int LogMarginVertical = 4;
    internal const int LogRadius = 4;
    internal const int PanelBottomMargin = 24;
    internal const int PanelHorizontalMargin = 28;
    internal const int PanelRadius = 8;
    internal const int PanelTopMargin = 24;
    internal const int ProgressBarHeight = 24;
    internal const string FontSize = "font_size";
    internal const string DefaultColor = "default_color";
    internal const string NormalFontSize = "normal_font_size";
    internal const string Panel = "panel";
    internal const string StateDisabled = "disabled";
    internal const string StateHover = "hover";
    internal const string StateNormal = "normal";
    internal const string StatePressed = "pressed";
    internal const string ThemeSeparation = "separation";

    internal static readonly Color ButtonDisabled = new(0.2f, 0.2f, 0.22f);
    internal static readonly Color ButtonHover = new(0.3f, 0.3f, 0.36f);
    internal static readonly Color ButtonNormal = new(0.25f, 0.25f, 0.3f);
    internal static readonly Color ButtonPressed = new(0.2f, 0.2f, 0.25f);
    internal static readonly Color DialogOverlay = new(0, 0, 0, 0.6f);
    internal static readonly Color DialogPanelBackground = new(0.15f, 0.15f, 0.18f);
    internal static readonly Color LogBackground = new(0.05f, 0.05f, 0.07f);
    internal static readonly Color LogText = new(0.6f, 0.6f, 0.65f);
    internal static readonly Color PanelBackground = new(0.12f, 0.12f, 0.15f);
    internal static readonly Color ScreenBackground = new(0.08f, 0.08f, 0.1f);

    internal static int ScaleInt(float scale, int value)
        => (int)(value * scale);
}
