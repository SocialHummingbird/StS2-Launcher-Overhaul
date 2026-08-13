using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private Button BuildUpdateSupportButton(float scale, bool compact, Container supportToolsParent)
    {
        var updateButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Check for updates",
                scale,
                InvokeVersionPrimaryAction
            )
            : AddPrimaryHiddenButton(
                supportToolsParent,
                "Check for Updates",
                scale,
                InvokeVersionPrimaryAction
            );
        LauncherButtonStyles.ApplyPrimaryAction(updateButton, scale);
        updateButton.AccessibilityName = "Check for updates";
        return updateButton;
    }

    private Button BuildRefreshVersionsSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Refresh list",
                scale,
                () => RefreshGameVersionsPressed?.Invoke()
            )
            : AddSecondaryHiddenButton(
                supportToolsParent,
                "Refresh list",
                scale,
                () => RefreshGameVersionsPressed?.Invoke()
            );

    private Button BuildRedownloadSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Repair current version",
                scale,
                () => RedownloadPressed?.Invoke()
            )
            : AddSecondaryHiddenButton(
                supportToolsParent,
                "Repair current version",
                scale,
                () => RedownloadPressed?.Invoke()
            );

    private Button BuildClearCachedVersionsSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Remove old versions...",
                scale,
                () => ClearCachedVersionsPressed?.Invoke()
            )
            : AddSecondaryHiddenButton(
                supportToolsParent,
                "Remove old versions...",
                scale,
                () => ClearCachedVersionsPressed?.Invoke()
            );

}
