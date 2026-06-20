using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Launcher;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed class DownloadSection : VBoxContainer
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

        _branchDetailsToggle = new StyledButton(
            compact ? "" : "Show Version Details",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_branchDetailsToggle, scale);
        _branchDetailsToggle.Visible = compact;
        _branchDetailsToggle.Pressed += ToggleBranchDetails;
        AddChild(_branchDetailsToggle);

        _compactSelectedVersionPanel = new Button
        {
            Text = "",
            ClipText = true,
            Visible = compact,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "Change game version for local files",
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(
                    _compactStackedActionRows
                        ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight
                        : LauncherSectionMetrics.CompactVersionSummaryHeight,
                    scale
                )
            ),
        };
        ApplySelectedVersionSummaryButtonStyle(_compactSelectedVersionPanel, scale, compact);
        _compactSelectedVersionPanel.Pressed += OpenCompactBranchDetailsFromSelectedVersion;
        AddChild(_compactSelectedVersionPanel);

        _compactSelectedVersionLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactVersionSummaryFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        )
        {
            VerticalAlignment = VerticalAlignment.Center,
        };
        _compactSelectedVersionLabel.AutowrapMode = _compactStackedActionRows
            ? TextServer.AutowrapMode.WordSmart
            : compact
                ? TextServer.AutowrapMode.Off
                : TextServer.AutowrapMode.WordSmart;
        _compactSelectedVersionLabel.ClipText = compact && !_compactStackedActionRows;
        if (compact && !_compactStackedActionRows)
            _compactSelectedVersionLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        if (compact)
        {
            _compactSelectedVersionLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(
                    _compactStackedActionRows
                        ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight
                        : LauncherSectionMetrics.CompactVersionSummaryHeight,
                    scale
                )
            );
        }
        _compactSelectedVersionLabel.MouseFilter = MouseFilterEnum.Ignore;
        _compactSelectedVersionLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _compactSelectedVersionLabel.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        _compactSelectedVersionLabel.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        _compactSelectedVersionLabel.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
        _compactSelectedVersionLabel.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
        _compactSelectedVersionLabel.Visible = compact;
        _compactSelectedVersionLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        _compactSelectedVersionPanel.AddChild(_compactSelectedVersionLabel);

        _branchDropdown = new OptionButton();
        _branchDropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _branchDropdown.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? LauncherSectionMetrics.PrimaryButtonHeight : LauncherSectionMetrics.SecondaryButtonHeight,
                scale
            )
        );
        LauncherButtonStyles.ApplyDropdownAction(
            _branchDropdown,
            scale,
            compact ? LauncherSectionMetrics.PrimaryButtonFontSize : LauncherSectionMetrics.SecondaryButtonFontSize,
            compact
        );
        _branchDropdown.ItemSelected += ApplyGameBranch;
        _compactVersionControlsRow = compact
            ? BuildCompactVersionControlsRow(scale, _compactStackedActionRows)
            : null;
        if (compact)
        {
            _compactVersionControlsRow.AddChild(_branchDropdown);
            AddChild(_compactVersionControlsRow);
        }
        else
        {
            AddChild(_branchDropdown);
        }

        _refreshBranchesButton = new StyledButton(
            compact ? "" : "Refresh Game Versions",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDetailButtonHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        _refreshBranchesButton.Pressed += () => RefreshGameVersionsRequested?.Invoke();
        if (compact)
        {
            SetCompactVersionActionButtonText(
                _refreshBranchesButton,
                "Refresh Versions",
                "Update branch list"
            );
            _compactVersionControlsRow.AddChild(_refreshBranchesButton);
        }
        else
        {
            AddChild(_refreshBranchesButton);
        }

        _branchHelpLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactVersionHelpFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        _branchHelpLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _branchHelpLabel.ClipText = compact;
        _branchHelpLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            _branchHelpLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _branchHelpLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactVersionHelpHeight, scale)
            );
        }
        _branchHelpLabel.MouseFilter = MouseFilterEnum.Ignore;
        _branchHelpLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        AddChild(_branchHelpLabel);

        SetGameBranch(_gameBranch);

        _downloadButton = new StyledButton(
            CompactDownloadButtonText(DefaultDownloadButtonText, compact),
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.PrimaryButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            height: compact
                ? CompactDownloadActionHeight
                : LauncherSectionMetrics.DownloadButtonHeight
        );
        SetCompactDownloadButtonText(_downloadButton, _downloadButton.Text);
        _downloadButton.Pressed += () => DownloadRequested?.Invoke();
        AddChild(_downloadButton);
        MoveCompactPrimaryInstallControlsBeforeVersionDetails();

        _progressBar = new StyledProgressBar(scale, compact);
        _progressBar.Visible = false;
        AddChild(_progressBar);

        _progressLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.SecondaryButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize
        );
        _progressLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _progressLabel.ClipText = compact;
        _progressLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            _progressLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _progressLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactDownloadProgressLabelHeight, scale)
            );
        }
        _progressLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            compact
                ? LauncherComponentTheme.CyanAccent
                : LauncherViewLayoutMetrics.LogTitleColor
        );
        _progressLabel.Visible = false;
        AddChild(_progressLabel);
        MoveCompactProgressControlsNearPrimaryAction();
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
        var normalizedBranch = SteamGameBranch.Normalize(branch);
        var branchChanged = !string.Equals(_gameBranch, normalizedBranch, StringComparison.OrdinalIgnoreCase);
        _gameBranch = normalizedBranch;
        PopulateBranchDropdown();
        if (branchChanged)
        {
            CollapseCompactBranchDetailsAfterSelection();
            return;
        }

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
        _branchHelpLabel.Text = _compact
            ? CompactInstallVersionHelpText()
            : SteamGameBranch.SelectorInstallSlotHelpText(_gameBranch)
                + "\n"
                + LauncherBranchCatalog.SelectedOptionStatus(_gameBranch, _availableBranches)
                + "\n"
                + "Download/update changes local files for the selected game version only; it does not change Steam Cloud saves.";
        ApplyBranchControlVisibility();
        if (_branchDetailsToggle != null)
        {
            if (_compact)
            {
                SetCompactVersionActionButtonText(
                    _branchDetailsToggle,
                    _branchDetailsExpanded ? "Hide Version" : "Change Version",
                    _branchDetailsExpanded ? "Keep selection" : "Local files only"
                );
            }
            else
            {
                _branchDetailsToggle.Text = _branchDetailsExpanded
                    ? "Hide Version Details"
                    : "Show Version Details";
            }
        }
        if (_compactSelectedVersionLabel != null)
        {
            _compactSelectedVersionLabel.Text = _compact
                ? CompactSelectedVersionHeadline()
                : $"Selected version: {SteamGameBranch.CompactDisplayName(_gameBranch, 22)}\n"
                    + $"Install slot: {SteamGameInstallPaths.VersionSlotKind(_gameBranch)}. Downloads do not change Steam Cloud saves.";
            _compactSelectedVersionLabel.Visible = _compact;
        }
        if (_compactSelectedVersionPanel != null)
            _compactSelectedVersionPanel.Visible = _compact;
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

    private void ApplyBranchControlVisibility()
    {
        var controlsVisible = !_compact || _branchDetailsExpanded;
        if (_compactVersionControlsRow != null)
            _compactVersionControlsRow.Visible = controlsVisible;
        _branchDropdown.Visible = controlsVisible;
        _refreshBranchesButton.Visible = controlsVisible;
        _branchHelpLabel.Visible = controlsVisible;
    }

    private void MoveCompactPrimaryInstallControlsBeforeVersionDetails()
    {
        if (!_compact)
            return;

        MoveChild(_compactSelectedVersionPanel, _branchDetailsToggle.GetIndex());
        MoveChild(_downloadButton, _branchDetailsToggle.GetIndex());
    }

    private void MoveCompactProgressControlsNearPrimaryAction()
    {
        if (!_compact)
            return;

        MoveChild(_progressLabel, _downloadButton.GetIndex() + 1);
        MoveChild(_progressBar, _progressLabel.GetIndex() + 1);
    }

    private static string CompactDownloadButtonText(string text, bool compact)
    {
        if (!compact)
            return text;

        var (title, detail) = CompactDownloadButtonTitleDetail(text);
        return $"{title}\n{detail}";
    }

    private static string CompactDownloadProgressButtonText()
        => "Downloading...\nSteam files";

    private static string CompactDownloadProgressText(string text)
    {
        var detail = CompactDownloadProgressDetail(text);
        return detail.Length == 0
            ? "Downloading selected version"
            : $"Downloading selected version\n{detail}";
    }

    private static string CompactDownloadProgressDetail(string text)
    {
        var normalized = NormalizeCompactProgressText(text);
        if (normalized.Length == 0)
            return "Waiting for Steam";

        if (normalized.Length <= CompactDownloadProgressDetailLimit)
            return normalized;

        return normalized[..Math.Max(0, CompactDownloadProgressDetailLimit - 3)].TrimEnd() + "...";
    }

    private static string NormalizeCompactProgressText(string text)
        => string.IsNullOrWhiteSpace(text)
            ? ""
            : string.Join(" ", text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));

    private static (string Title, string Detail) CompactDownloadButtonTitleDetail(string text)
    {
        var normalized = (text ?? "").Trim();
        if (normalized.Length == 0
            || string.Equals(normalized, DefaultDownloadButtonText, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "DOWNLOAD SELECTED VERSION", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "DOWNLOAD VERSION", StringComparison.OrdinalIgnoreCase))
            return ("Download Version", "Local files only");

        if (string.Equals(normalized, "REDOWNLOAD SELECTED VERSION", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "REDOWNLOAD VERSION", StringComparison.OrdinalIgnoreCase))
            return ("Redownload Version", "Rebuild local files");

        if (string.Equals(normalized, "RETRY DOWNLOAD", StringComparison.OrdinalIgnoreCase))
            return ("Retry Download", "Local files only");

        if (string.Equals(normalized, "DOWNLOADING...", StringComparison.OrdinalIgnoreCase))
            return ("Downloading...", "Steam files");

        return (normalized, "Local files only");
    }

    private static Container BuildCompactVersionControlsRow(float scale, bool compactStackedActionRows)
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.Visible = false;
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );
        return row;
    }

    private static void ApplySelectedVersionSummaryButtonStyle(Button button, float scale, bool compact)
    {
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            BuildSelectedVersionSummaryStyle(scale, compact)
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateHover,
            BuildSelectedVersionSummaryStyle(
                scale,
                compact,
                new Color(0.045f, 0.085f, 0.095f, 0.95f),
                new Color(0.04f, 0.72f, 0.8f, 0.78f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StatePressed,
            BuildSelectedVersionSummaryStyle(
                scale,
                compact,
                new Color(0.025f, 0.05f, 0.06f, 0.98f),
                new Color(0.95f, 0.42f, 0.08f, 0.72f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateDisabled,
            BuildSelectedVersionSummaryStyle(
                scale,
                compact,
                new Color(0.025f, 0.04f, 0.048f, 0.58f),
                new Color(0.05f, 0.22f, 0.26f, 0.32f)
            )
        );
    }

    private static StyleBoxFlat BuildSelectedVersionSummaryStyle(float scale, bool compact)
        => BuildSelectedVersionSummaryStyle(
            scale,
            compact,
            new Color(0.035f, 0.065f, 0.075f, 0.9f),
            new Color(0.04f, 0.55f, 0.62f, 0.65f)
        );

    private static StyleBoxFlat BuildSelectedVersionSummaryStyle(
        float scale,
        bool compact,
        Color body,
        Color border
    )
    {
        var style = LauncherStyleBoxes.MakeFilled(
            body,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? LauncherSectionMetrics.CompactVersionSummaryRadius : 8,
                scale
            )
        );
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12,
            scale
        );
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12,
            scale
        );
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 9,
            scale
        );
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(
            compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 10,
            scale
        );
        return style;
    }

    private string CompactSelectedVersionHeadline()
    {
        if (_compactStackedActionRows)
        {
            return $"Version: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactSelectedVersionStackedBranchLimit)}\n"
                + $"{CompactInstallFileScope(_gameBranch)} | Cloud unchanged | Change version";
        }

        return $"Version: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactSelectedVersionBranchLimit)} | {CompactInstallFileScope(_gameBranch)} | Cloud unchanged | Change";
    }

    private string CompactInstallVersionHelpText()
    {
        var branchLimit = _compactStackedActionRows
            ? CompactVersionHelpStackedBranchLimit
            : CompactVersionHelpBranchLimit;

        return $"Files for: {SteamGameBranch.CompactDisplayName(_gameBranch, branchLimit)} | {CompactInstallFileScope(_gameBranch)}\n"
            + $"{LauncherBranchCatalog.SelectedOptionCompactStatus(_gameBranch, _availableBranches)} | Saves unchanged";
    }

    private static string CompactInstallFileScope(string branch)
        => string.Equals(SteamGameBranch.Normalize(branch), SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? "Default files"
            : "Separate files";

    private void SetCompactVersionActionButtonText(Button button, string title, string detail)
    {
        if (!_compact)
        {
            button.Text = title;
            return;
        }

        var labels = EnsureCompactVersionActionButtonLabels(button);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactVersionActionButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactVersionActionBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactVersionActionTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactVersionActionDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactVersionActionBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionHorizontalMargin, _scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionHorizontalMargin, _scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionVerticalMargin, _scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactVersionActionVerticalMargin, _scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );

        var title = new StyledLabel(
            "",
            _scale,
            fontSize: CompactVersionActionTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactVersionActionTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detail = new StyledLabel(
            "",
            _scale,
            fontSize: CompactVersionActionDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactVersionActionDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detail.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detail);

        button.AddChild(body);
        return (body, title, detail);
    }

    private void SetCompactDownloadButtonText(Button button, string text)
    {
        if (!_compact || !TrySplitCompactDownloadButtonText(text, out var title, out var detail))
        {
            HideCompactDownloadButtonLabels(button);
            button.Text = text;
            return;
        }

        var labels = EnsureCompactDownloadButtonLabels(button);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private static bool TrySplitCompactDownloadButtonText(
        string text,
        out string title,
        out string detail
    )
    {
        title = text ?? "";
        detail = "";
        var separator = title.IndexOf('\n');
        if (separator < 0)
            return false;

        detail = title[(separator + 1)..].Trim();
        title = title[..separator].Trim();
        return title.Length > 0 && detail.Length > 0;
    }

    private static void HideCompactDownloadButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactDownloadActionBodyName));
        if (body != null)
            body.Visible = false;
    }

    private (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactDownloadButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactDownloadActionBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactDownloadActionTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactDownloadActionDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactDownloadActionBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactDownloadActionHorizontalMargin, _scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactDownloadActionHorizontalMargin, _scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactDownloadActionVerticalMargin, _scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactDownloadActionVerticalMargin, _scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );

        var title = new StyledLabel(
            "",
            _scale,
            fontSize: CompactDownloadActionTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactDownloadActionTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detail = new StyledLabel(
            "",
            _scale,
            fontSize: CompactDownloadActionDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactDownloadActionDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detail.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detail);

        button.AddChild(body);
        return (body, title, detail);
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

    private void ApplyGameBranch(long index)
    {
        if (index < 0 || index >= _branchOptions.Count)
            return;

        var branch = _branchOptions[(int)index].Branch;
        SetGameBranch(branch);
        CollapseCompactBranchDetailsAfterSelection();
        GameBranchChanged?.Invoke(_gameBranch);
    }

    private void CollapseCompactBranchDetailsAfterSelection()
    {
        if (!_compact)
            return;

        _branchDetailsExpanded = false;
        UpdateBranchHelpText();
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

    private void OpenCompactBranchDetailsFromSelectedVersion()
    {
        if (!_compact)
            return;

        _branchDetailsExpanded = true;
        UpdateBranchHelpText();
    }
}
