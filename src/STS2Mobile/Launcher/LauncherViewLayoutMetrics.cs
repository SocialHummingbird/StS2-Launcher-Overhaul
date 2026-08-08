using System;
using Godot;

namespace STS2Mobile.Launcher;

internal static class LauncherViewLayoutMetrics
{
    internal const float LogColumnStretchRatio = 3f;
    internal const float PrimaryColumnStretchRatio = 2f;
    internal const int LogTitleFontSize = 14;
    internal const int PrimaryColumnMinWidth = 420;
    internal const int CompactPrimaryColumnSeparation = 8;
    internal const int CompactRootColumnSeparation = 8;
    internal const int PrimaryColumnSeparation = 10;
    internal const int RootColumnSeparation = 16;
    internal const string ThemeFontColor = "font_color";
    internal const string ThemeSeparation = "separation";

    internal static readonly Color LogTitleColor = new(0.6f, 0.6f, 0.65f);

    internal static int ScaleInt(int value, float scale)
        => Math.Max(0, (int)MathF.Round(value * scale, MidpointRounding.AwayFromZero));
}
