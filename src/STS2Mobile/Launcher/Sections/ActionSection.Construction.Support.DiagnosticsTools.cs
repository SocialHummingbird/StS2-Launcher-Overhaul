using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private Button BuildDiagnosticsSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Create support report",
                scale,
                () => DiagnosticsPressed?.Invoke()
            )
            : AddSecondaryHiddenButton(
                supportToolsParent,
                "Create support report",
                scale,
                () => DiagnosticsPressed?.Invoke()
            );

    private Button BuildShowLastErrorSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "View last error",
                scale,
                () => ShowLastErrorPressed?.Invoke()
            )
            : AddSecondaryHiddenButton(
                supportToolsParent,
                "View last error",
                scale,
                () => ShowLastErrorPressed?.Invoke()
            );

    private Button BuildCopyRawLogSupportButton(float scale, bool compact, Container supportToolsParent)
        => compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Copy launcher log",
                scale,
                () => CopyRawLogPressed?.Invoke()
            )
            : AddSecondaryHiddenButton(
                supportToolsParent,
                "Copy launcher log",
                scale,
                () => CopyRawLogPressed?.Invoke()
            );
}
