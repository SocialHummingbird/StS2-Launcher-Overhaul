using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private static readonly CompactButtonDetailLabelSpec CompactDownloadActionLabels = new(
        CompactDownloadActionBodyName,
        CompactDownloadActionTitleName,
        CompactDownloadActionDetailName,
        CompactDownloadActionTitleFontSize,
        CompactDownloadActionDetailFontSize,
        CompactDownloadActionHorizontalMargin,
        CompactDownloadActionVerticalMargin
    );

    private void SetCompactDownloadButtonText(Button button, string text)
        => CompactButtonDetailLabels.Apply(
            button,
            text,
            _scale,
            _compact,
            CompactDownloadActionLabels
        );
}
