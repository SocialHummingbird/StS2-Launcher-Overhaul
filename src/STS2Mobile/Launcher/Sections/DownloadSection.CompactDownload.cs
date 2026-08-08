namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private void MoveCompactProgressControlsNearPrimaryAction()
    {
        if (!_compact)
            return;

        MoveChild(_progressLabel, _downloadButton.GetIndex() + 1);
        MoveChild(_progressBar, _progressLabel.GetIndex() + 1);
    }
}
