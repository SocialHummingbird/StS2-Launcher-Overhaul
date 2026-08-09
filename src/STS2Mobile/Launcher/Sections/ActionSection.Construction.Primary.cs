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
            compact ? CompactRetryButtonText() : "Retry",
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
        LauncherButtonStyles.ApplyPrimaryAction(launchButton, scale);
        var safeLaunchButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke(),
                "Compatibility mode"
            )
            : AddSecondaryHiddenButton(
                this,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke()
            );
        LauncherButtonStyles.ApplySafeAction(safeLaunchButton, scale);
        safeLaunchButton.AccessibilityDescription =
            "Start with the project renderer and skip shader warmup for this run.";

        return (retryButton, launchButton, safeLaunchButton);
    }
}
