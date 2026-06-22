using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private SupportControls BuildSupportControls(float scale, bool compact, Container supportToolsParent)
    {
        var supportToggle = BuildSupportToggle(scale, compact);
        AddChild(_supportGroup);

        return new SupportControls(
            supportToggle,
            BuildUpdateSupportButton(scale, compact, supportToolsParent),
            BuildRefreshVersionsSupportButton(scale, compact, supportToolsParent),
            BuildRedownloadSupportButton(scale, compact, supportToolsParent),
            BuildClearCachedVersionsSupportButton(scale, compact, supportToolsParent),
            BuildWorkshopSyncSupportButton(scale, compact, supportToolsParent),
            BuildWorkshopClearSupportButton(scale, compact, supportToolsParent),
            BuildDiagnosticsSupportButton(scale, compact, supportToolsParent),
            BuildShowLastErrorSupportButton(scale, compact, supportToolsParent),
            BuildCopyRawLogSupportButton(scale, compact, supportToolsParent)
        );
    }

    private Button BuildSupportToggle(float scale, bool compact)
    {
        var supportToggle = AddHiddenButton(
            this,
            SupportToggleText(),
            scale,
            compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight,
            ToggleSupportOptions
        );
        LauncherButtonStyles.ApplySupportAction(supportToggle, scale);
        SetCompactActionButtonText(supportToggle, supportToggle.Text);
        return supportToggle;
    }
}
