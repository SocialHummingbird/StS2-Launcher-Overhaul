using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly struct SupportControls
    {
        internal SupportControls(
            Button updateButton,
            Button refreshVersionsButton,
            Button redownloadButton,
            Button clearCachedVersionsButton,
            Button diagnosticsButton,
            Button showLastErrorButton,
            Button copyRawLogButton
        )
        {
            UpdateButton = updateButton;
            RefreshVersionsButton = refreshVersionsButton;
            RedownloadButton = redownloadButton;
            ClearCachedVersionsButton = clearCachedVersionsButton;
            DiagnosticsButton = diagnosticsButton;
            ShowLastErrorButton = showLastErrorButton;
            CopyRawLogButton = copyRawLogButton;
        }

        internal Button UpdateButton { get; }
        internal Button RefreshVersionsButton { get; }
        internal Button RedownloadButton { get; }
        internal Button ClearCachedVersionsButton { get; }
        internal Button DiagnosticsButton { get; }
        internal Button ShowLastErrorButton { get; }
        internal Button CopyRawLogButton { get; }
    }
}
