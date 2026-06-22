using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly struct SupportFoundation
    {
        internal SupportFoundation(VBoxContainer group, GridContainer toolsGrid, Container toolsParent)
        {
            Group = group;
            ToolsGrid = toolsGrid;
            ToolsParent = toolsParent;
        }

        internal VBoxContainer Group { get; }
        internal GridContainer ToolsGrid { get; }
        internal Container ToolsParent { get; }
    }

    private readonly struct SupportControls
    {
        internal SupportControls(
            Button supportToggle,
            Button updateButton,
            Button refreshVersionsButton,
            Button redownloadButton,
            Button clearCachedVersionsButton,
            Button workshopSyncButton,
            Button workshopClearButton,
            Button diagnosticsButton,
            Button showLastErrorButton,
            Button copyRawLogButton
        )
        {
            SupportToggle = supportToggle;
            UpdateButton = updateButton;
            RefreshVersionsButton = refreshVersionsButton;
            RedownloadButton = redownloadButton;
            ClearCachedVersionsButton = clearCachedVersionsButton;
            WorkshopSyncButton = workshopSyncButton;
            WorkshopClearButton = workshopClearButton;
            DiagnosticsButton = diagnosticsButton;
            ShowLastErrorButton = showLastErrorButton;
            CopyRawLogButton = copyRawLogButton;
        }

        internal Button SupportToggle { get; }
        internal Button UpdateButton { get; }
        internal Button RefreshVersionsButton { get; }
        internal Button RedownloadButton { get; }
        internal Button ClearCachedVersionsButton { get; }
        internal Button WorkshopSyncButton { get; }
        internal Button WorkshopClearButton { get; }
        internal Button DiagnosticsButton { get; }
        internal Button ShowLastErrorButton { get; }
        internal Button CopyRawLogButton { get; }
    }
}
