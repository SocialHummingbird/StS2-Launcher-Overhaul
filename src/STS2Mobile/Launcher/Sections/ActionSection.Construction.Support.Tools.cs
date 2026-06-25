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
                "Check Files",
                scale,
                () => CheckForUpdatesPressed?.Invoke(),
                "Updates"
            )
            : AddPrimaryHiddenButton(
                _supportGroup,
                "Check for Updates",
                scale,
                () => CheckForUpdatesPressed?.Invoke()
            );
        LauncherButtonStyles.ApplySupportAction(updateButton, scale);
        return updateButton;
    }

    private Button BuildRefreshVersionsSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Game Versions",
                scale,
                () => RefreshGameVersionsPressed?.Invoke(),
                "Refresh list"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Refresh Game Versions",
                scale,
                () => RefreshGameVersionsPressed?.Invoke()
            );

    private Button BuildRedownloadSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Repair Files",
                scale,
                () => RedownloadPressed?.Invoke(),
                "Rebuild game"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Redownload Selected Version",
                scale,
                () => RedownloadPressed?.Invoke()
            );

    private Button BuildClearCachedVersionsSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Free Space",
                scale,
                () => ClearCachedVersionsPressed?.Invoke(),
                "Old versions"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Clear Cached Versions",
                scale,
                () => ClearCachedVersionsPressed?.Invoke()
            );

}
