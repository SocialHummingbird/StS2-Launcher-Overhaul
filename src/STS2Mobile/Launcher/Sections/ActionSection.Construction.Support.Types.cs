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
            Button diagnosticsButton,
            Button showLastErrorButton,
            Button copyRawLogButton
        )
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
