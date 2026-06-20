namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    internal void SetProgress(double pct, string text)
    {
        ShowProgress(pct, text);
    }

    internal void ShowProgress(string text)
    {
        _downloadButton.Disabled = true;
        ShowProgress(0, text);
    }

    internal void HideProgress()
    {
        _progressBar.Visible = false;
        _progressLabel.Visible = false;
    }

    internal void Reset(string buttonText = DefaultDownloadButtonText)
    {
        _downloadButton.Disabled = false;
        _branchDropdown.Disabled = false;
        _refreshBranchesButton.Disabled = false;
        if (_compactSelectedVersionPanel != null)
            _compactSelectedVersionPanel.Disabled = false;
        ApplyBranchControlVisibility();
        SetCompactDownloadButtonText(_downloadButton, CompactDownloadButtonText(buttonText, _compact));
        HideProgress();
        _progressBar.Value = 0;
    }

    private void ShowProgress(double pct, string text)
    {
        if (_compact)
        {
            _branchDetailsExpanded = false;
            ApplyBranchControlVisibility();
            UpdateBranchHelpText();
            _compactSelectedVersionPanel.Disabled = true;
        }

        _progressBar.Visible = true;
        _progressBar.Value = pct;
        _progressLabel.Visible = true;
        _progressLabel.Text = _compact ? CompactDownloadProgressText(text) : text;
        if (_compact)
            SetCompactDownloadButtonText(_downloadButton, CompactDownloadProgressButtonText());
        _branchDropdown.Disabled = true;
        _refreshBranchesButton.Disabled = true;
    }
}
