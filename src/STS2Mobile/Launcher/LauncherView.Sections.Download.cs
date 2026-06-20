using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void ShowDownloadAction(string buttonText)
    {
        SetFirstRunGuideVisible(false);
        HideCompactCompletedAuthSections(showCode: false);
        SetCompactReadyInstallSectionVisible(true);
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        SetCompactCurrentTask("Files", Download, "Download version");
        Download.Visible = true;
        Download.Reset(buttonText);
        ScrollCompactPrimaryTo(Download);
    }

    internal void SetGameBranch(string branch)
    {
        Download.SetGameBranch(branch);
        Actions.SetGameBranch(branch);
    }

    internal void SetGameBranchOptions(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        Download.SetAvailableBranches(branches);
        Actions.SetAvailableBranches(branches);
    }

    internal void ShowDownloadProgress(string text)
    {
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        SetCompactCurrentTask("Files", Download, "Download version");
        Download.ShowProgress(text);
    }

    internal void SetDownloadProgress(double percentage, string text)
    {
        SetCompactWorkflowStep(CompactWorkflowStep.Files);
        Download.SetProgress(percentage, text);
    }

    internal void HideDownload()
        => Download.Visible = false;

    internal void ResetDownload()
        => Download.Reset();

    internal void ResetDownload(string buttonText)
        => Download.Reset(buttonText);

    internal void SetDownloadButtonDisabled(bool disabled)
        => Download.SetButtonDisabled(disabled);

    internal void SetRefreshGameVersionsBusy(bool busy)
    {
        Download.SetRefreshVersionsButtonDisabled(busy);
        Actions.SetRefreshVersionsButtonDisabled(busy);
    }
}
