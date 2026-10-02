using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher;
using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class DownloadSection
{
    private Button BuildBranchDetailsToggle(float scale, bool compact)
    {
        var button = new StyledButton(compact ? "" : "Show Version Details", scale, fontSize: compact ? LauncherSectionMetrics.CompactDetailButtonFontSize : LauncherSectionMetrics.ProgressFontSize, height: compact ? LauncherSectionMetrics.CompactDrawerToggleHeight : LauncherSectionMetrics.SecondaryButtonHeight);
        LauncherButtonStyles.ApplySupportAction(button, scale);
        button.Visible = compact;
        button.Pressed += ToggleBranchDetails;
        return button;
    }

    private static readonly CompactButtonDetailLabelSpec CompactVersionActionLabels = CompactButtonDetailLabelSpec.Default(CompactVersionActionBodyName, CompactVersionActionTitleName, CompactVersionActionDetailName);
    private void SetCompactVersionActionButtonText(Button button, string title, string detail)
    {
        if (!_compact)
        {
            CompactButtonDetailLabels.Apply(button, title, _scale, enabled: false, CompactVersionActionLabels);
            return;
        }

        CompactButtonDetailLabels.Apply(button, $"{title}\n{detail}", _scale, enabled: true, CompactVersionActionLabels);
    }

    private void MoveCompactPrimaryInstallControlsBeforeVersionDetails()
    {
        if (!_compact)
            return;
        MoveChild(_compactSelectedVersionPanel, _branchDetailsToggle.GetIndex());
        MoveChild(_downloadButton, _branchDetailsToggle.GetIndex());
    }

    private static Container BuildCompactVersionControlsRow(float scale, bool compactStackedActionRows)
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.Visible = false;
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(6, scale));
        return row;
    }

    private string CompactSelectedVersionHeadline()
    {
        if (_compactStackedActionRows)
        {
            return $"Version: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactSelectedVersionStackedBranchLimit)}\n" + $"{CompactInstallFileScope(_gameBranch)} | Change version";
        }

        return $"Version: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactSelectedVersionBranchLimit)} | {CompactInstallFileScope(_gameBranch)} | Change";
    }

    private string CompactInstallVersionHelpText()
    {
        var branchLimit = _compactStackedActionRows ? CompactVersionHelpStackedBranchLimit : CompactVersionHelpBranchLimit;
        return $"Files for: {SteamGameBranch.CompactDisplayName(_gameBranch, branchLimit)} | {CompactInstallFileScope(_gameBranch)}\n" + LauncherBranchCatalog.SelectedOptionCompactStatus(_gameBranch, _availableBranches) + "\nChecked before replacement; saves and other branches stay in place";
    }

    private static string CompactInstallFileScope(string branch) => string.Equals(SteamGameBranch.Normalize(branch), SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase) ? "Default files" : "Separate files";
    private static void ApplySelectedVersionSummaryButtonStyle(Button button, float scale, bool compact)
    {
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, BuildSelectedVersionSummaryStyle(scale, compact));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateHover, BuildSelectedVersionSummaryStyle(scale, compact, new Color(0.045f, 0.085f, 0.095f, 0.95f), new Color(0.04f, 0.72f, 0.8f, 0.78f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StatePressed, BuildSelectedVersionSummaryStyle(scale, compact, new Color(0.025f, 0.05f, 0.06f, 0.98f), new Color(0.95f, 0.42f, 0.08f, 0.72f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateDisabled, BuildSelectedVersionSummaryStyle(scale, compact, new Color(0.025f, 0.04f, 0.048f, 0.58f), new Color(0.05f, 0.22f, 0.26f, 0.32f)));
    }

    private static StyleBoxFlat BuildSelectedVersionSummaryStyle(float scale, bool compact) => BuildSelectedVersionSummaryStyle(scale, compact, new Color(0.035f, 0.065f, 0.075f, 0.9f), new Color(0.04f, 0.55f, 0.62f, 0.65f));
    private static StyleBoxFlat BuildSelectedVersionSummaryStyle(float scale, bool compact, Color body, Color border)
    {
        var style = LauncherStyleBoxes.MakeFilled(body, LauncherViewLayoutMetrics.ScaleInt(compact ? LauncherSectionMetrics.CompactVersionSummaryRadius : 8, scale));
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin : 12, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 9, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? LauncherSectionMetrics.CompactVersionSummaryVerticalMargin : 10, scale);
        return style;
    }

    private OptionButton BuildBranchDropdown(float scale, bool compact)
    {
        var dropdown = new OptionButton
        {
            FitToLongestItem = !compact,
        };
        dropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        dropdown.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(compact ? LauncherSectionMetrics.PrimaryButtonHeight : LauncherSectionMetrics.SecondaryButtonHeight, scale));
        LauncherButtonStyles.ApplyDropdownAction(dropdown, scale, compact ? LauncherSectionMetrics.PrimaryButtonFontSize : LauncherSectionMetrics.SecondaryButtonFontSize, compact);
        dropdown.ItemSelected += ApplyGameBranch;
        return dropdown;
    }

    private void AddBranchDropdownToLayout()
    {
        if (_compact)
        {
            _compactVersionControlsRow.AddChild(_branchDropdown);
            AddChild(_compactVersionControlsRow);
            return;
        }

        AddChild(_branchDropdown);
    }

    private Button BuildRefreshBranchesButton(float scale, bool compact)
    {
        var button = new StyledButton(compact ? "" : "Refresh Game Versions", scale, fontSize: compact ? LauncherSectionMetrics.CompactDetailButtonFontSize : LauncherSectionMetrics.SecondaryButtonFontSize, height: compact ? LauncherSectionMetrics.CompactDetailButtonHeight : LauncherSectionMetrics.SecondaryButtonHeight);
        button.Pressed += () => RefreshGameVersionsRequested?.Invoke();
        if (compact)
        {
            SetCompactVersionActionButtonText(button, "Refresh Versions", "Update branch list");
        }

        return button;
    }

    private void AddRefreshBranchesButtonToLayout()
    {
        if (_compact)
        {
            _compactVersionControlsRow.AddChild(_refreshBranchesButton);
            return;
        }

        AddChild(_refreshBranchesButton);
    }

    private Label BuildBranchHelpLabel(float scale, bool compact)
    {
        var label = new StyledLabel("", scale, fontSize: compact ? CompactVersionHelpFontSize : LauncherSectionMetrics.ProgressFontSize, align: HorizontalAlignment.Left);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = compact;
        label.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactVersionHelpHeight, scale));
        }

        label.MouseFilter = MouseFilterEnum.Ignore;
        label.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherViewLayoutMetrics.LogTitleColor);
        return label;
    }

    private Button BuildCompactSelectedVersionPanel(float scale, bool compact)
    {
        var button = new Button
        {
            Text = "",
            ClipText = true,
            Visible = compact,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "Change game version for local files",
            CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(_compactStackedActionRows ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight : LauncherSectionMetrics.CompactVersionSummaryHeight, scale)),
        };
        ApplySelectedVersionSummaryButtonStyle(button, scale, compact);
        button.Pressed += OpenCompactBranchDetailsFromSelectedVersion;
        return button;
    }

    private Label BuildCompactSelectedVersionLabel(float scale, bool compact)
    {
        var label = new StyledLabel("", scale, fontSize: compact ? LauncherSectionMetrics.CompactVersionSummaryFontSize : LauncherSectionMetrics.ProgressFontSize, align: HorizontalAlignment.Left)
        {
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AutowrapMode = _compactStackedActionRows ? TextServer.AutowrapMode.WordSmart : compact ? TextServer.AutowrapMode.Off : TextServer.AutowrapMode.WordSmart;
        label.ClipText = compact && !_compactStackedActionRows;
        if (compact && !_compactStackedActionRows)
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        if (compact)
        {
            label.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(_compactStackedActionRows ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight : LauncherSectionMetrics.CompactVersionSummaryHeight, scale));
        }

        label.MouseFilter = MouseFilterEnum.Ignore;
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin, scale);
        label.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin, scale);
        label.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.CompactVersionSummaryVerticalMargin, scale);
        label.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.CompactVersionSummaryVerticalMargin, scale);
        label.Visible = compact;
        label.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextSecondary);
        return label;
    }
}
