using Godot;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const string CompactDiagnosticsToggleBodyName = "CompactDiagnosticsToggleBody";
    private const string CompactDiagnosticsToggleTitleName = "CompactDiagnosticsToggleTitle";
    private const string CompactDiagnosticsToggleDetailName = "CompactDiagnosticsToggleDetail";

    private static readonly CompactButtonDetailLabelSpec CompactDiagnosticsToggleLabels =
        CompactButtonDetailLabelSpec.Default(
            CompactDiagnosticsToggleBodyName,
            CompactDiagnosticsToggleTitleName,
            CompactDiagnosticsToggleDetailName
        );

    private static void SetDiagnosticsToggleText(
        Button toggle,
        LauncherLayoutProfile profile,
        bool visible
    )
    {
        if (!profile.Compact)
        {
            CompactButtonDetailLabels.Apply(
                toggle,
                DiagnosticsToggleText(visible),
                profile.Scale,
                enabled: false,
                CompactDiagnosticsToggleLabels
            );
            return;
        }

        CompactButtonDetailLabels.Apply(
            toggle,
            visible ? "Hide Help\nBack to launcher" : "Help & Reports\nPrivate until opened",
            profile.Scale,
            enabled: true,
            CompactDiagnosticsToggleLabels
        );
    }

    private static string DiagnosticsToggleText(bool visible)
        => visible ? "Hide Help & Reports" : "Show Help & Reports";
}
