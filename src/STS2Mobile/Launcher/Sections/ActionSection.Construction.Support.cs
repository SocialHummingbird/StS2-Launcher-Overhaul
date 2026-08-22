using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private SupportControls BuildSupportControls(float scale, bool compact, Container supportToolsParent)
        => new(
            BuildUpdateSupportButton(scale, compact, supportToolsParent),
            BuildRefreshVersionsSupportButton(scale, compact, supportToolsParent),
            BuildRedownloadSupportButton(scale, compact, supportToolsParent),
            BuildDiagnosticsSupportButton(scale, compact, supportToolsParent),
            BuildShowLastErrorSupportButton(scale, compact, supportToolsParent),
            BuildCopyRawLogSupportButton(scale, compact, supportToolsParent)
        );
}
