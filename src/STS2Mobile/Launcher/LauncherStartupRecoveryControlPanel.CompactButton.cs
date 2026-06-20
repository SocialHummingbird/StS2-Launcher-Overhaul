using Godot;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const string CompactRecoveryButtonBodyName = "CompactRecoveryButtonBody";
    private const string CompactRecoveryButtonTitleName = "CompactRecoveryButtonTitle";
    private const string CompactRecoveryButtonDetailName = "CompactRecoveryButtonDetail";
    private const int CompactRecoveryButtonTitleFontSize = 16;
    private const int CompactRecoveryButtonDetailFontSize = 12;
    private const int CompactRecoveryButtonHorizontalMargin = 8;
    private const int CompactRecoveryButtonVerticalMargin = 6;

    private static readonly CompactButtonDetailLabelSpec CompactRecoveryButtonLabels = new(
        CompactRecoveryButtonBodyName,
        CompactRecoveryButtonTitleName,
        CompactRecoveryButtonDetailName,
        CompactRecoveryButtonTitleFontSize,
        CompactRecoveryButtonDetailFontSize,
        CompactRecoveryButtonHorizontalMargin,
        CompactRecoveryButtonVerticalMargin
    );

    private static void AddCompactRecoveryButtonLabels(
        Button button,
        float scale,
        string titleText,
        string detailText
    )
        => CompactButtonDetailLabels.Apply(
            button,
            $"{titleText}\n{detailText}",
            scale,
            enabled: true,
            CompactRecoveryButtonLabels
        );
}
