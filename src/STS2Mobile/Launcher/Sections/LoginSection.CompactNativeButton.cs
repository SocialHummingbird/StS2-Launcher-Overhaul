using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class LoginSection
{
    private const int CompactNativeLoginButtonHeight = LauncherSectionMetrics.CodeInputHeight;

    private static readonly CompactButtonDetailLabelSpec CompactNativeLoginLabels = new(
        "CompactNativeLoginBody",
        "CompactNativeLoginTitle",
        "CompactNativeLoginDetail",
        LauncherSectionMetrics.CompactDetailButtonFontSize,
        LauncherSectionMetrics.CompactDetailLabelFontSize,
        horizontalMargin: 6,
        verticalMargin: 4
    );

    private static string CompactNativeLoginText()
        => "Sign in with Steam\nAndroid login";

    private static void SetCompactNativeLoginButtonText(
        Button button,
        string text,
        float scale,
        bool compactNativeLogin
    )
        => CompactButtonDetailLabels.Apply(
            button,
            text,
            scale,
            compactNativeLogin,
            CompactNativeLoginLabels
        );
}
