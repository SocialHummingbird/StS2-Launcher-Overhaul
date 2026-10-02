using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class ActionSection
{
    private SupportControls BuildSupportControls(float scale, bool compact, Container supportToolsParent) => new(BuildUpdateSupportButton(scale, compact, supportToolsParent), BuildRefreshVersionsSupportButton(scale, compact, supportToolsParent), BuildRedownloadSupportButton(scale, compact, supportToolsParent), BuildDiagnosticsSupportButton(scale, compact, supportToolsParent), BuildShowLastErrorSupportButton(scale, compact, supportToolsParent), BuildCopyRawLogSupportButton(scale, compact, supportToolsParent));
    private Button BuildDiagnosticsSupportButton(float scale, bool compact, Container supportToolsParent) => compact ? AddCompactSupportToolButton(supportToolsParent, "Create support report", scale, () => DiagnosticsPressed?.Invoke()) : AddSecondaryHiddenButton(supportToolsParent, "Create support report", scale, () => DiagnosticsPressed?.Invoke());
    private Button BuildShowLastErrorSupportButton(float scale, bool compact, Container supportToolsParent) => compact ? AddCompactSupportToolButton(supportToolsParent, "View last error", scale, () => ShowLastErrorPressed?.Invoke()) : AddSecondaryHiddenButton(supportToolsParent, "View last error", scale, () => ShowLastErrorPressed?.Invoke());
    private Button BuildCopyRawLogSupportButton(float scale, bool compact, Container supportToolsParent) => compact ? AddCompactSupportToolButton(supportToolsParent, "Copy launcher log", scale, () => CopyRawLogPressed?.Invoke()) : AddSecondaryHiddenButton(supportToolsParent, "Copy launcher log", scale, () => CopyRawLogPressed?.Invoke());
    private Button BuildUpdateSupportButton(float scale, bool compact, Container supportToolsParent)
    {
        var updateButton = compact ? AddCompactSupportToolButton(supportToolsParent, "Check for updates", scale, InvokeVersionPrimaryAction) : AddPrimaryHiddenButton(supportToolsParent, "Check for Updates", scale, InvokeVersionPrimaryAction);
        LauncherButtonStyles.ApplyPrimaryAction(updateButton, scale);
        updateButton.AccessibilityName = "Check for updates";
        return updateButton;
    }

    private Button BuildRefreshVersionsSupportButton(float scale, bool compact, Container supportToolsParent) => compact ? AddCompactSupportToolButton(supportToolsParent, "Refresh list", scale, () => RefreshGameVersionsPressed?.Invoke()) : AddSecondaryHiddenButton(supportToolsParent, "Refresh list", scale, () => RefreshGameVersionsPressed?.Invoke());
    private Button BuildRedownloadSupportButton(float scale, bool compact, Container supportToolsParent) => compact ? AddCompactSupportToolButton(supportToolsParent, "Redownload selected version", scale, () => RedownloadPressed?.Invoke()) : AddSecondaryHiddenButton(supportToolsParent, "Redownload selected version", scale, () => RedownloadPressed?.Invoke());
    private readonly struct SupportControls
    {
        internal SupportControls(Button updateButton, Button refreshVersionsButton, Button redownloadButton, Button diagnosticsButton, Button showLastErrorButton, Button copyRawLogButton)
        {
            UpdateButton = updateButton;
            RefreshVersionsButton = refreshVersionsButton;
            RedownloadButton = redownloadButton;
            DiagnosticsButton = diagnosticsButton;
            ShowLastErrorButton = showLastErrorButton;
            CopyRawLogButton = copyRawLogButton;
        }

        internal Button UpdateButton { get; }
        internal Button RefreshVersionsButton { get; }
        internal Button RedownloadButton { get; }
        internal Button DiagnosticsButton { get; }
        internal Button ShowLastErrorButton { get; }
        internal Button CopyRawLogButton { get; }
    }
}
