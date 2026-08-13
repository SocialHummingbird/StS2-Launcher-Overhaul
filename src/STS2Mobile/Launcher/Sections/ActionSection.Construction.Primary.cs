using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private (Button RetryButton, Button LaunchButton, Button SafeLaunchButton) BuildPrimaryActionControls(
        float scale,
        bool compact,
        Container supportToolsParent
    )
    {
        var retryButton = AddHiddenButton(
            this,
            "Try Again",
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            () => RetryPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(retryButton, scale);
        SetCompactActionButtonText(retryButton, retryButton.Text);

        var launchButton = AddPrimaryHiddenButton(
            this,
            "Play",
            scale,
            () => LaunchPressed?.Invoke()
        );
        launchButton.Name = "Play";
        launchButton.AccessibilityName = "Play";
        LauncherButtonStyles.ApplyPrimaryAction(launchButton, scale);
        var safeLaunchButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke()
            )
            : AddSecondaryHiddenButton(
                this,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke()
            );
        LauncherButtonStyles.ApplyPrimaryAction(safeLaunchButton, scale);
        safeLaunchButton.AccessibilityName = "Safe Start";
        safeLaunchButton.AccessibilityDescription =
            "Uses local saves and skips shader warmup for one run. Uses OpenGL on PowerVR; otherwise uses the game's default renderer.";

        return (retryButton, launchButton, safeLaunchButton);
    }
}
