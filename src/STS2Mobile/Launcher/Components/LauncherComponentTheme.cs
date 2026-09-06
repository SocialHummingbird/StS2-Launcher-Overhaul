using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal static class LauncherComponentTheme
{
    internal const int ButtonDefaultFontSize = 16;
    internal const int ButtonDefaultHeight = 54;
    internal const int ButtonRadius = 12;
    internal const int DialogButtonFontSize = 16;
    internal const int DialogButtonHeight = 58;
    internal const int DialogButtonWidth = 156;
    internal const int DialogButtonSeparation = 12;
    internal const int DialogContentSeparation = 16;
    internal const int DialogMessageFontSize = 16;
    internal const int DialogMessageWidth = 440;
    internal const int DialogPanelMargin = 24;
    internal const int DialogPanelRadius = 18;
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
    internal const int PanelRadius = 18;
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

    internal static readonly Color ButtonDisabled = new("#20252F");
    internal static readonly Color ButtonHover = new("#303B4D");
    internal static readonly Color ButtonNormal = new("#1B2330");
    internal static readonly Color ButtonPressed = new("#111923");
    internal static readonly Color BrandGlowCyan = new(0.02f, 0.62f, 0.72f, 0.16f);
    internal static readonly Color BrandGlowOrange = new(1.0f, 0.28f, 0.02f, 0.14f);
    internal static readonly Color CyanAccent = new("#A6C7C4");
    internal static readonly Color CyanDim = new("#3B515A");
    internal static readonly Color DarkInk = new("#171A21");
    internal static readonly Color DialogOverlay = new(0, 0, 0, 0.6f);
    internal static readonly Color DialogPanelBackground = new("#171F2B");
    internal static readonly Color LineEditBackground = new("#0D131C");
    internal static readonly Color LineEditFocusBorder = new("#E7BB78");
    internal static readonly Color LineEditNormalBorder = new("#344051");
    internal static readonly Color LogBackground = new(0.02f, 0.04f, 0.06f);
    internal static readonly Color LogText = new(0.55f, 0.78f, 0.82f);
    internal static readonly Color OrangeAccent = new("#E7BB78");
    internal static readonly Color OrangeHot = new("#F5D7A3");
    internal static readonly Color PanelBackground = new("#111720");
    internal static readonly Color ProgressBackground = new("#202B39");
    internal static readonly Color ProgressFill = new("#86B5AF");
    internal static readonly Color ProgressFillCompact = new("#A6C7C4");
    internal static readonly Color ScreenBackground = new("#090D14");
    internal static readonly Color TextMuted = new("#8994A5");
    internal static readonly Color TextPrimary = new("#F3F1EC");
    internal static readonly Color TextSecondary = new("#B0BAC9");

    internal static int ScaleInt(float scale, int value)
        => Math.Max(0, (int)MathF.Round(value * scale, MidpointRounding.AwayFromZero));
}
