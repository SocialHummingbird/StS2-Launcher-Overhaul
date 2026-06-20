using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private static readonly CompactButtonDetailLabelSpec CompactVersionActionLabels =
        CompactButtonDetailLabelSpec.Default(
            CompactVersionActionBodyName,
            CompactVersionActionTitleName,
            CompactVersionActionDetailName
        );

    private void SetCompactVersionActionButtonText(Button button, string title, string detail)
    {
        if (!_compact)
        {
            CompactButtonDetailLabels.Apply(
                button,
                title,
                _scale,
                enabled: false,
                CompactVersionActionLabels
            );
            return;
        }

        CompactButtonDetailLabels.Apply(
            button,
            $"{title}\n{detail}",
            _scale,
            enabled: true,
            CompactVersionActionLabels
        );
    }
}
