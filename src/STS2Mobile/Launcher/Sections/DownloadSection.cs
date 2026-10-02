using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class DownloadSection : VBoxContainer
{
    private const string DefaultDownloadButtonText = "Download Game Files";
    private const int CompactSelectedVersionBranchLimit = 18;
    private const int CompactSelectedVersionStackedBranchLimit = 28;
    private const int CompactVersionHelpBranchLimit = 22;
    private const int CompactVersionHelpStackedBranchLimit = 30;
    private const int CompactVersionHelpHeight = 54;
    private const int CompactVersionHelpFontSize = LauncherSectionMetrics.CompactVersionSummaryFontSize;
    private const string CompactVersionActionBodyName = "CompactVersionActionBody";
    private const string CompactVersionActionTitleName = "CompactVersionActionTitle";
    private const string CompactVersionActionDetailName = "CompactVersionActionDetail";
    private const int CompactDownloadActionHeight = LauncherSectionMetrics.CodeInputHeight;
    private const string CompactDownloadActionBodyName = "CompactDownloadActionBody";
    private const string CompactDownloadActionTitleName = "CompactDownloadActionTitle";
    private const string CompactDownloadActionDetailName = "CompactDownloadActionDetail";
    private const int CompactDownloadActionTitleFontSize = LauncherSectionMetrics.PrimaryButtonFontSize;
    private const int CompactDownloadActionDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactDownloadActionHorizontalMargin = 8;
    private const int CompactDownloadActionVerticalMargin = 6;
    private const int CompactDownloadProgressLabelHeight = 50;
    private const int CompactDownloadProgressDetailLimit = 54;
    internal event Action DownloadRequested;
    internal event Action<string> GameBranchChanged;
    internal event Action RefreshGameVersionsRequested;
    private readonly OptionButton _branchDropdown;
    private readonly Button _refreshBranchesButton;
    private readonly Container _compactVersionControlsRow;
    private readonly Label _branchHelpLabel;
    private readonly Button _branchDetailsToggle;
    private readonly Button _compactSelectedVersionPanel;
    private readonly Label _compactSelectedVersionLabel;
    private readonly Button _downloadButton;
    private readonly ProgressBar _progressBar;
    private readonly Label _progressLabel;
    private readonly List<LauncherBranchCatalog.BranchOption> _branchOptions = new();
    private IReadOnlyList<LauncherBranchCatalog.BranchOption> _availableBranches = Array.Empty<LauncherBranchCatalog.BranchOption>();
    private readonly float _scale;
    private readonly bool _compact;
    private readonly bool _compactStackedActionRows;
    private bool _branchDetailsExpanded;
    private string _gameBranch = SteamGameBranch.Public;
    internal DownloadSection(float scale, bool compact = false, bool compactStackedActionRows = false)
    {
        _scale = scale;
        _compact = compact;
        _compactStackedActionRows = compact && compactStackedActionRows;
        LauncherSectionSetup.ConfigureHiddenSection(this, scale, "Game Install", "Choose the Steam branch, download files, and keep separate installs isolated.", LauncherComponentTheme.CyanAccent, compact, "Local files");
        _branchDetailsToggle = BuildBranchDetailsToggle(scale, compact);
        AddChild(_branchDetailsToggle);
        _compactSelectedVersionPanel = BuildCompactSelectedVersionPanel(scale, compact);
        AddChild(_compactSelectedVersionPanel);
        _compactSelectedVersionLabel = BuildCompactSelectedVersionLabel(scale, compact);
        _compactSelectedVersionPanel.AddChild(_compactSelectedVersionLabel);
        _branchDropdown = BuildBranchDropdown(scale, compact);
        _compactVersionControlsRow = compact ? BuildCompactVersionControlsRow(scale, _compactStackedActionRows) : null;
        AddBranchDropdownToLayout();
        _refreshBranchesButton = BuildRefreshBranchesButton(scale, compact);
        AddRefreshBranchesButtonToLayout();
        _branchHelpLabel = BuildBranchHelpLabel(scale, compact);
        AddChild(_branchHelpLabel);
        SetGameBranch(_gameBranch);
        _downloadButton = BuildDownloadButton(scale, compact);
        AddChild(_downloadButton);
        MoveCompactPrimaryInstallControlsBeforeVersionDetails();
        _progressBar = BuildProgressBar(scale, compact);
        AddChild(_progressBar);
        _progressLabel = BuildProgressLabel(scale, compact);
        AddChild(_progressLabel);
        MoveCompactProgressControlsNearPrimaryAction();
    }

    internal void SetButtonDisabled(bool disabled) => _downloadButton.Disabled = disabled;
    internal void SetRefreshVersionsButtonDisabled(bool disabled) => _refreshBranchesButton.Disabled = disabled;
    private Button BuildDownloadButton(float scale, bool compact)
    {
        var button = new StyledButton(CompactDownloadButtonText(DefaultDownloadButtonText, compact), scale, fontSize: compact ? LauncherSectionMetrics.PrimaryButtonFontSize : LauncherSectionMetrics.SecondaryButtonFontSize, height: compact ? CompactDownloadActionHeight : LauncherSectionMetrics.DownloadButtonHeight);
        button.Name = "DownloadGameFilesAction";
        LauncherButtonStyles.ApplyPrimaryAction(button, scale);
        SetCompactDownloadButtonText(button, button.Text);
        button.Pressed += () => DownloadRequested?.Invoke();
        return button;
    }

    private static ProgressBar BuildProgressBar(float scale, bool compact)
    {
        var progress = new StyledProgressBar(scale, compact);
        progress.Visible = false;
        return progress;
    }

    private Label BuildProgressLabel(float scale, bool compact)
    {
        var label = new StyledLabel("", scale, fontSize: compact ? LauncherSectionMetrics.SecondaryButtonFontSize : LauncherSectionMetrics.ProgressFontSize);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = compact;
        label.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactDownloadProgressLabelHeight, scale));
        }

        label.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, compact ? LauncherComponentTheme.CyanAccent : LauncherViewLayoutMetrics.LogTitleColor);
        label.Visible = false;
        return label;
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
        {
            SetCompactDownloadButtonText(_downloadButton, CompactDownloadProgressButtonText(text));
        }

        _branchDropdown.Disabled = true;
        _refreshBranchesButton.Disabled = true;
    }
}
