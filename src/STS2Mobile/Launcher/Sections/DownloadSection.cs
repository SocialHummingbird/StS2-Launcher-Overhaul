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
    private const int CompactVersionActionTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactVersionActionDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactVersionActionHorizontalMargin = 6;
    private const int CompactVersionActionVerticalMargin = 4;
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
        LauncherSectionSetup.ConfigureHiddenSection(
            this,
            scale,
            "Game Install",
            "Choose the Steam branch, download files, and keep separate installs isolated.",
            LauncherComponentTheme.CyanAccent,
            compact,
            "Local files"
        );

        _branchDetailsToggle = BuildBranchDetailsToggle(scale, compact);
        AddChild(_branchDetailsToggle);

        _compactSelectedVersionPanel = BuildCompactSelectedVersionPanel(scale, compact);
        AddChild(_compactSelectedVersionPanel);

        _compactSelectedVersionLabel = BuildCompactSelectedVersionLabel(scale, compact);
        _compactSelectedVersionPanel.AddChild(_compactSelectedVersionLabel);

        _branchDropdown = BuildBranchDropdown(scale, compact);
        _compactVersionControlsRow = compact
            ? BuildCompactVersionControlsRow(scale, _compactStackedActionRows)
            : null;
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

    internal void SetRefreshVersionsButtonDisabled(bool disabled)
        => _refreshBranchesButton.Disabled = disabled;
}
