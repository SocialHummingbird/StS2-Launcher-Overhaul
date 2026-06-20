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
            "Start Game",
            scale,
            () => LaunchPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(launchButton, scale);
        var safeLaunchButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke(),
                "Cloud off"
            )
            : AddSecondaryHiddenButton(
                this,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke()
            );
        LauncherButtonStyles.ApplySafeAction(safeLaunchButton, scale);

        return (retryButton, launchButton, safeLaunchButton);
    }
}
