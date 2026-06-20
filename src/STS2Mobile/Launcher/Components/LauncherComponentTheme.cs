using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal static class LauncherComponentTheme
{
    internal const int ButtonDefaultFontSize = 16;
    internal const int ButtonDefaultHeight = 54;
    internal const int ButtonRadius = 8;
    internal const int DialogButtonFontSize = 16;
    internal const int DialogButtonHeight = 58;
    internal const int DialogButtonWidth = 156;
    internal const int DialogButtonSeparation = 12;
    internal const int DialogContentSeparation = 16;
    internal const int DialogMessageFontSize = 16;
    internal const int DialogMessageWidth = 440;
    internal const int DialogPanelMargin = 24;
    internal const int DialogPanelRadius = 8;
    internal const int LineEditFontSize = 16;
    internal const int LineEditHeight = 52;
    internal const int LineEditHorizontalPadding = 14;
    internal const int LineEditRadius = 8;
    internal const int LogFontSize = 13;
    internal const int LogHeight = 150;
    internal const int LogMarginHorizontal = 10;
    internal const int LogMarginVertical = 8;
    internal const int LogRadius = 6;
    internal const int PanelBottomMargin = 24;
    internal const int PanelHorizontalMargin = 28;
    internal const int PanelRadius = 8;
    internal const int PanelTopMargin = 24;
    internal const int ProgressBarHeight = 24;
    internal const int CompactProgressBarHeight = 34;
    internal const int ProgressBarFontSize = 12;
    internal const int CompactProgressBarFontSize = 14;
    internal const int ProgressBarRadius = 6;
    internal const string FontSize = "font_size";
    internal const string DefaultColor = "default_color";
    internal const string NormalFontSize = "normal_font_size";
    internal const string Panel = "panel";
    internal const string StateDisabled = "disabled";
    internal const string StateHover = "hover";
    internal const string StateNormal = "normal";
    internal const string StatePressed = "pressed";
    internal const string ThemeSeparation = "separation";

    internal static readonly Color ButtonDisabled = new(0.12f, 0.14f, 0.17f);
    internal static readonly Color ButtonHover = new(0.13f, 0.22f, 0.3f);
    internal static readonly Color ButtonNormal = new(0.08f, 0.14f, 0.2f);
    internal static readonly Color ButtonPressed = new(0.05f, 0.1f, 0.16f);
    internal static readonly Color BrandGlowCyan = new(0.02f, 0.62f, 0.72f, 0.16f);
    internal static readonly Color BrandGlowOrange = new(1.0f, 0.28f, 0.02f, 0.14f);
    internal static readonly Color CyanAccent = new(0.04f, 0.84f, 0.95f);
    internal static readonly Color CyanDim = new(0.03f, 0.42f, 0.52f);
    internal static readonly Color DarkInk = new(0.02f, 0.04f, 0.06f);
    internal static readonly Color DialogOverlay = new(0, 0, 0, 0.6f);
    internal static readonly Color DialogPanelBackground = new(0.07f, 0.1f, 0.13f);
    internal static readonly Color LineEditBackground = new(0.025f, 0.045f, 0.065f);
    internal static readonly Color LineEditFocusBorder = new(0.04f, 0.84f, 0.95f, 0.85f);
    internal static readonly Color LineEditNormalBorder = new(0.05f, 0.23f, 0.3f, 0.8f);
    internal static readonly Color LogBackground = new(0.02f, 0.04f, 0.06f);
    internal static readonly Color LogText = new(0.55f, 0.78f, 0.82f);
    internal static readonly Color OrangeAccent = new(1.0f, 0.48f, 0.02f);
    internal static readonly Color OrangeHot = new(1.0f, 0.78f, 0.08f);
    internal static readonly Color PanelBackground = new(0.045f, 0.07f, 0.095f);
    internal static readonly Color ProgressBackground = new(0.02f, 0.035f, 0.045f);
    internal static readonly Color ProgressFill = new(0.04f, 0.62f, 0.72f);
    internal static readonly Color ProgressFillCompact = new(0.04f, 0.84f, 0.95f);
    internal static readonly Color ScreenBackground = new(0.015f, 0.02f, 0.03f);
    internal static readonly Color TextMuted = new(0.45f, 0.52f, 0.56f);
    internal static readonly Color TextPrimary = new(0.9f, 0.95f, 0.93f);
    internal static readonly Color TextSecondary = new(0.58f, 0.74f, 0.76f);

    internal static int ScaleInt(float scale, int value)
        => Math.Max(0, (int)MathF.Round(value * scale, MidpointRounding.AwayFromZero));
}
