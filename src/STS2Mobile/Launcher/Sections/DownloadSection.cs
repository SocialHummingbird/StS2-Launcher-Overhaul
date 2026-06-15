using System;
using System.Collections.Generic;
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
    internal event Action RefreshGameVersionsRequested;

    private readonly OptionButton _branchDropdown;
    private readonly Button _refreshBranchesButton;
    private readonly Label _branchHelpLabel;
    private readonly Button _branchDetailsToggle;
    private readonly Button _downloadButton;
    private readonly ProgressBar _progressBar;
    private readonly Label _progressLabel;
    private readonly List<LauncherBranchCatalog.BranchOption> _branchOptions = new();
    private IReadOnlyList<LauncherBranchCatalog.BranchOption> _availableBranches = Array.Empty<LauncherBranchCatalog.BranchOption>();
    private readonly bool _compact;
    private bool _branchDetailsExpanded;
    private string _gameBranch = SteamGameBranch.Public;

    internal DownloadSection(float scale, bool compact = false)
    {
        _compact = compact;
        LauncherSectionSetup.ConfigureHiddenSection(
            this,
            scale,
            "Game Install",
            "Choose the Steam branch, download files, and keep separate installs isolated.",
            LauncherComponentTheme.CyanAccent,
            compact
        );

        _branchDropdown = new OptionButton();
        _branchDropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _branchDropdown.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SecondaryButtonHeight, scale)
        );
        _branchDropdown.ItemSelected += ApplyGameBranch;
        AddChild(_branchDropdown);

        _refreshBranchesButton = new StyledButton(
            compact ? "REFRESH VERSIONS" : "REFRESH GAME VERSIONS",
            scale,
            fontSize: LauncherSectionMetrics.SecondaryButtonFontSize,
            height: LauncherSectionMetrics.SecondaryButtonHeight
        );
        _refreshBranchesButton.Pressed += () => RefreshGameVersionsRequested?.Invoke();
        AddChild(_refreshBranchesButton);

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

        _branchDetailsToggle = new StyledButton(
            "SHOW VERSION DETAILS",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            height: LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_branchDetailsToggle, scale);
        _branchDetailsToggle.Visible = compact;
        _branchDetailsToggle.Pressed += ToggleBranchDetails;
        AddChild(_branchDetailsToggle);
        SetGameBranch(_gameBranch);

        _downloadButton = new StyledButton(
            CompactDownloadButtonText(DefaultDownloadButtonText, compact),
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

    internal void SetRefreshVersionsButtonDisabled(bool disabled)
        => _refreshBranchesButton.Disabled = disabled;

    internal void SetGameBranch(string branch)
    {
        _gameBranch = SteamGameBranch.Normalize(branch);
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    internal void SetAvailableBranches(IReadOnlyList<LauncherBranchCatalog.BranchOption> branches)
    {
        _availableBranches = branches ?? Array.Empty<LauncherBranchCatalog.BranchOption>();
        PopulateBranchDropdown();
        UpdateBranchHelpText();
    }

    private void UpdateBranchHelpText()
    {
        _branchHelpLabel.Text = SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch)
            + "\n"
            + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches)
            + "\n"
            + "Download/update changes local files for the selected game version only; it does not change Steam Cloud saves.";
        _branchHelpLabel.Visible = !_compact || _branchDetailsExpanded;
        if (_branchDetailsToggle != null)
        {
            _branchDetailsToggle.Text = _branchDetailsExpanded
                ? "HIDE VERSION DETAILS"
                : "SHOW VERSION DETAILS";
        }
    }

    internal void Reset(string buttonText = DefaultDownloadButtonText)
    {
        _downloadButton.Disabled = false;
        _branchDropdown.Disabled = false;
        _refreshBranchesButton.Disabled = false;
        _downloadButton.Text = CompactDownloadButtonText(buttonText, _compact);
        HideProgress();
        _progressBar.Value = 0;
    }

    private static string CompactDownloadButtonText(string text, bool compact)
    {
        if (!compact)
            return text;

        return string.Equals(text, DefaultDownloadButtonText, StringComparison.OrdinalIgnoreCase)
            ? "DOWNLOAD"
            : text;
    }

    private void ShowProgress(double pct, string text)
    {
        _progressBar.Visible = true;
        _progressBar.Value = pct;
        _progressLabel.Visible = true;
        _progressLabel.Text = text;
        _branchDropdown.Disabled = true;
        _refreshBranchesButton.Disabled = true;
    }

    private void ApplyGameBranch(long index)
    {
        if (index < 0 || index >= _branchOptions.Count)
            return;

        var branch = _branchOptions[(int)index].Branch;
        SetGameBranch(branch);
        GameBranchChanged?.Invoke(_gameBranch);
    }

    private void PopulateBranchDropdown()
    {
        _branchOptions.Clear();
        _branchDropdown.Clear();

        var selectedIndex = 0;
        foreach (var option in LauncherBranchCatalog.DropdownOptions(_gameBranch, _availableBranches))
        {
            var index = _branchOptions.Count;
            _branchOptions.Add(option);
            _branchDropdown.AddItem(option.Label);

            if (string.Equals(option.Branch, _gameBranch, StringComparison.OrdinalIgnoreCase))
                selectedIndex = index;
        }

        _branchDropdown.Select(selectedIndex);
    }

    private void ToggleBranchDetails()
    {
        _branchDetailsExpanded = !_branchDetailsExpanded;
        UpdateBranchHelpText();
    }
}
