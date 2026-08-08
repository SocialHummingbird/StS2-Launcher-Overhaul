using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private Button BuildDiagnosticsSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Help Report",
                scale,
                () => DiagnosticsPressed?.Invoke(),
                "Share details"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Create Help Report",
                scale,
                () => DiagnosticsPressed?.Invoke()
            );

    private Button BuildShowLastErrorSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Last Problem",
                scale,
                () => ShowLastErrorPressed?.Invoke(),
                "Open details"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Show Last Problem",
                scale,
                () => ShowLastErrorPressed?.Invoke()
            );

    private Button BuildCopyRawLogSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Copy Log",
                scale,
                () => CopyRawLogPressed?.Invoke(),
                "Review first"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Copy Launcher Log (Review First)",
                scale,
                () => CopyRawLogPressed?.Invoke()
            );
}
