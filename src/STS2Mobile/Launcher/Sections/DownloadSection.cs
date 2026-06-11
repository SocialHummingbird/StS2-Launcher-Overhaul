using System;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed class DownloadSection : VBoxContainer
{
    private const string DefaultDownloadButtonText = "DOWNLOAD GAME FILES";

    internal event Action DownloadRequested;
    internal event Action<string> GameBranchChanged;

    private readonly Button _branchButton;
    private readonly Label _branchHelpLabel;
    private readonly Button _downloadButton;
    private readonly ProgressBar _progressBar;
    private readonly Label _progressLabel;
    private string _gameBranch = SteamGameBranch.Public;

    internal DownloadSection(float scale)
    {
        LauncherSectionSetup.ConfigureHiddenSection(this, scale);

        _branchButton = new StyledButton(
            "",
            scale,
            fontSize: LauncherSectionMetrics.SecondaryButtonFontSize,
            height: LauncherSectionMetrics.SecondaryButtonHeight
        );
        _branchButton.Pressed += ToggleGameBranch;
        AddChild(_branchButton);

        _branchHelpLabel = new StyledLabel(
            "",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        _branchHelpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _branchHelpLabel.MouseFilter = MouseFilterEnum.Ignore;
        _branchHelpLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        AddChild(_branchHelpLabel);
        SetGameBranch(_gameBranch);

        _downloadButton = new StyledButton(
            DefaultDownloadButtonText,
            scale,
            height: LauncherSectionMetrics.DownloadButtonHeight
        );
        _downloadButton.Pressed += () => DownloadRequested?.Invoke();
        AddChild(_downloadButton);

        _progressBar = new StyledProgressBar(scale);
        _progressBar.Visible = false;
        AddChild(_progressBar);

        _progressLabel = new StyledLabel(
            "",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize
        );
        _progressLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        _progressLabel.Visible = false;
        AddChild(_progressLabel);
    }

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

    internal void SetButtonDisabled(bool disabled) => _downloadButton.Disabled = disabled;

    internal void SetGameBranch(string branch)
    {
        _gameBranch = SteamGameBranch.Normalize(branch);
        _branchButton.Text = $"GAME VERSION: {SteamGameBranch.DisplayName(_gameBranch)}";
        _branchHelpLabel.Text = SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch);
    }

    internal void Reset(string buttonText = DefaultDownloadButtonText)
    {
        _downloadButton.Disabled = false;
        _branchButton.Disabled = false;
        _downloadButton.Text = buttonText;
        HideProgress();
        _progressBar.Value = 0;
    }

    private void ShowProgress(double pct, string text)
    {
        _progressBar.Visible = true;
        _progressBar.Value = pct;
        _progressLabel.Visible = true;
        _progressLabel.Text = text;
        _branchButton.Disabled = true;
    }

    private void ToggleGameBranch()
    {
        SetGameBranch(SteamGameBranch.ToggleKnownBranch(_gameBranch));
        GameBranchChanged?.Invoke(_gameBranch);
    }
}
