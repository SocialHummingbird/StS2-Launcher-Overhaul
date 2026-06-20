using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal ActionSection(float scale, bool compact = false, bool compactStackedActionRows = false)
    {
        _scale = scale;
        _compact = compact;
        _compactStackedActionRows = compact && compactStackedActionRows;
        LauncherSectionSetup.ConfigureHiddenSection(
            this,
            scale,
            "Play and Sync",
            "Launch, update, switch versions, and move cloud saves only when you choose.",
            LauncherComponentTheme.OrangeHot,
            compact
        );

        var toggleRadius = (int)(4 * scale);
        var toggleBorderWidth = Math.Max(1, (int)(2 * scale));
        _toggleOffStyle = LauncherStyleBoxes.MakeOutline(
            new Color(0.7f, 0.25f, 0.25f),
            toggleRadius,
            toggleBorderWidth
        );
        _toggleOnStyle = LauncherStyleBoxes.MakeOutline(
            new Color(0.25f, 0.65f, 0.3f),
            toggleRadius,
            toggleBorderWidth
        );
        _supportGroup = BuildActionGroup(scale);
        _supportGroup.Visible = false;
        _supportToolsGrid = BuildCompactSupportToolsGrid(scale, compact, _compactStackedActionRows);
        if (compact)
            _supportGroup.AddChild(_supportToolsGrid);
        var supportToolsParent = compact
            ? (Container)_supportToolsGrid
            : _supportGroup;

        _retryButton = AddHiddenButton(
            this,
            compact ? CompactRetryButtonText() : "RETRY",
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            () => RetryPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(_retryButton, scale);
        SetCompactActionButtonText(_retryButton, _retryButton.Text);

        _launchButton = AddPrimaryHiddenButton(
            this,
            "START GAME",
            scale,
            () => LaunchPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(_launchButton, scale);
        _safeLaunchButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "SAFE START",
                scale,
                () => SafeLaunchPressed?.Invoke(),
                "Backup launch"
            )
            : AddSecondaryHiddenButton(
                this,
                "SAFE START",
                scale,
                () => SafeLaunchPressed?.Invoke()
            );
        LauncherButtonStyles.ApplySafeAction(_safeLaunchButton, scale);

        _branchDetailsToggle = new StyledButton(
            "SHOW VERSION DETAILS",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_branchDetailsToggle, scale);
        _branchDetailsToggle.Visible = false;
        _branchDetailsToggle.Pressed += ToggleBranchDetails;
        AddChild(_branchDetailsToggle);

        _branchDropdown = new OptionButton();
        _branchDropdown.Visible = false;
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
        AddChild(_branchDropdown);

        _branchHelpLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactReadyVersionHelpFontSize
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
                LauncherViewLayoutMetrics.ScaleInt(CompactReadyVersionHelpHeight, scale)
            );
        }
        _branchHelpLabel.MouseFilter = MouseFilterEnum.Ignore;
        _branchHelpLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherViewLayoutMetrics.LogTitleColor
        );
        _branchHelpLabel.Visible = false;
        AddChild(_branchHelpLabel);

        _readyVersionSummaryPanel = new PanelContainer
        {
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _readyVersionSummaryPanel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildReadyVersionSummaryStyle(scale, compact)
        );
        AddChild(_readyVersionSummaryPanel);

        _readyVersionSummaryLabel = new StyledLabel(
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
        _readyVersionSummaryLabel.AutowrapMode = _compactStackedActionRows
            ? TextServer.AutowrapMode.WordSmart
            : compact
            ? TextServer.AutowrapMode.Off
            : TextServer.AutowrapMode.WordSmart;
        _readyVersionSummaryLabel.ClipText = compact && !_compactStackedActionRows;
        if (compact && !_compactStackedActionRows)
            _readyVersionSummaryLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        if (compact)
        {
            _readyVersionSummaryLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(
                    _compactStackedActionRows
                        ? LauncherSectionMetrics.CompactStackedVersionSummaryHeight
                        : LauncherSectionMetrics.CompactVersionSummaryHeight,
                    scale
                )
            );
        }
        _readyVersionSummaryLabel.MouseFilter = MouseFilterEnum.Ignore;
        _readyVersionSummaryLabel.Visible = true;
        _readyVersionSummaryLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        _readyVersionSummaryPanel.AddChild(_readyVersionSummaryLabel);

        SetGameBranch(_gameBranch);

        _cloudGroup = BuildActionGroup(scale);
        _cloudGroup.Visible = false;
        AddChild(_cloudGroup);
        // Compact mode should make the Pull-first controls precede launch.
        if (compact)
            MoveChild(_launchButton, GetChildCount() - 1);

        _pushPullRow = new VBoxContainer();
        _pushPullRow.Visible = false;
        _pushPullRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                compact ? CompactCloudPrimaryActionSeparation : LauncherSectionMetrics.PushPullRowSeparation,
                scale
            )
        );
        Container cloudPrimaryActionsParent = compact
            ? BuildCompactCloudPrimaryActionsRow(_pushPullRow, scale, _compactStackedActionRows)
            : _pushPullRow;
        _pullButton = AddPushPullButton(
            cloudPrimaryActionsParent,
            compact ? CompactCloudPullText() : "Pull Saves from Steam Cloud",
            scale,
            () =>
            {
                ResetCloudPushArm();
                CloudPullPressed?.Invoke();
            }
        );
        LauncherButtonStyles.ApplyCloudPullAction(_pullButton, scale);
        SetCompactActionButtonText(_pullButton, _pullButton.Text);
        _cloudPushToggle = AddPushPullButton(
            cloudPrimaryActionsParent,
            compact ? CompactCloudPushToggleText(expanded: false) : "PUSH LOCKED",
            scale,
            ToggleCloudPush
        );
        LauncherButtonStyles.ApplyDangerAction(_cloudPushToggle, scale);
        SetCompactActionButtonText(_cloudPushToggle, _cloudPushToggle.Text);
        _cloudPushToggle.Visible = compact;
        _pushButton = AddPushPullButton(
            _pushPullRow,
            compact ? CompactCloudPushDangerText() : PushButtonText,
            scale,
            ArmCloudPush
        );
        LauncherButtonStyles.ApplyDangerAction(_pushButton, scale);
        SetCompactActionButtonText(_pushButton, _pushButton.Text);
        _confirmPushButton = AddPushPullButton(
            _pushPullRow,
            compact ? CompactCloudPushConfirmText() : PushConfirmButtonText,
            scale,
            ConfirmCloudPush
        );
        _confirmPushButton.Visible = false;
        LauncherButtonStyles.ApplyDangerAction(_confirmPushButton, scale);
        SetCompactActionButtonText(_confirmPushButton, _confirmPushButton.Text);
        _pushConfirmationLabel = new StyledLabel(
            compact
                ? CompactCloudPushWarningText()
                : "Confirming Push uploads Android saves to Steam Cloud for the selected version and can overwrite remote Steam Cloud saves. Continue only after Pull and local save evidence are verified.",
            scale,
            fontSize: compact
                ? CompactCloudPushWarningFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        _pushConfirmationLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _pushConfirmationLabel.ClipText = compact;
        _pushConfirmationLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            _pushConfirmationLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _pushConfirmationLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactCloudPushWarningHeight, scale)
            );
        }
        _pushConfirmationLabel.MouseFilter = MouseFilterEnum.Ignore;
        _pushConfirmationLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.OrangeHot
        );
        _pushConfirmationLabel.Visible = false;
        _pushPullRow.AddChild(_pushConfirmationLabel);
        _cloudGroup.AddChild(_pushPullRow);

        _cloudSafetyLabel = new StyledLabel(
            "",
            scale,
            fontSize: compact
                ? CompactCloudSafetyDetailFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        _cloudSafetyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cloudSafetyLabel.ClipText = compact;
        _cloudSafetyLabel.VerticalAlignment = VerticalAlignment.Center;
        if (compact)
        {
            _cloudSafetyLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _cloudSafetyLabel.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactCloudSafetyDetailHeight, scale)
            );
        }
        _cloudSafetyLabel.MouseFilter = MouseFilterEnum.Ignore;
        _cloudSafetyLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.OrangeHot
        );
        _cloudGroup.AddChild(_cloudSafetyLabel);

        _cloudSafetyToggle = new StyledButton(
            CompactCloudSafetySummary(),
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_cloudSafetyToggle, scale);
        SetCompactActionButtonText(_cloudSafetyToggle, _cloudSafetyToggle.Text);
        _cloudSafetyToggle.Visible = compact;
        _cloudSafetyToggle.Pressed += ToggleCloudSafety;
        _cloudGroup.AddChild(_cloudSafetyToggle);
        MoveCompactCloudSafetyCueBeforeCloudActions();

        _cloudOptionsToggle = new StyledButton(
            "SHOW CLOUD OPTIONS",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            height: compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_cloudOptionsToggle, scale);
        SetCompactActionButtonText(_cloudOptionsToggle, _cloudOptionsToggle.Text);
        _cloudOptionsToggle.Visible = compact;
        _cloudOptionsToggle.Pressed += ToggleCloudOptions;
        _cloudGroup.AddChild(_cloudOptionsToggle);

        Container cloudOptionsParent = _cloudGroup;
        if (compact)
        {
            _compactCloudOptionsRow = BuildCompactCloudOptionsRow(_cloudGroup, scale, _compactStackedActionRows);
            cloudOptionsParent = _compactCloudOptionsRow;
        }
        _localBackupToggle = compact
            ? AddCompactSupportToolButton(cloudOptionsParent, "BACKUP OFF", scale, null)
            : AddSecondaryHiddenButton(_cloudGroup, "Local Backup: OFF", scale, null);
        _cloudSyncToggle = compact
            ? AddCompactSupportToolButton(cloudOptionsParent, "SYNC OFF", scale, null)
            : AddSecondaryHiddenButton(_cloudGroup, "Game Cloud Sync: OFF", scale, null);
        ConfigureLocalBackupToggle();
        ConfigureCloudSyncToggle();
        UpdateBranchHelpText();

        _supportToggle = AddHiddenButton(
            this,
            SupportToggleText(),
            scale,
            compact
                ? LauncherSectionMetrics.CompactDetailButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            compact
                ? LauncherSectionMetrics.CompactDrawerToggleHeight
                : LauncherSectionMetrics.SecondaryButtonHeight,
            ToggleSupportOptions
        );
        LauncherButtonStyles.ApplySupportAction(_supportToggle, scale);
        SetCompactActionButtonText(_supportToggle, _supportToggle.Text);

        AddChild(_supportGroup);

        _updateButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "UPDATES",
                scale,
                () => CheckForUpdatesPressed?.Invoke(),
                "Check files"
            )
            : AddPrimaryHiddenButton(
                _supportGroup,
                "CHECK FOR UPDATES",
                scale,
                () => CheckForUpdatesPressed?.Invoke()
            );
        LauncherButtonStyles.ApplySupportAction(_updateButton, scale);
        _refreshVersionsButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "VERSIONS",
                scale,
                () => RefreshGameVersionsPressed?.Invoke(),
                "Refresh list"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "REFRESH GAME VERSIONS",
                scale,
                () => RefreshGameVersionsPressed?.Invoke()
            );
        _redownloadButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "REDOWNLOAD",
                scale,
                () => RedownloadPressed?.Invoke(),
                "Rebuild slot"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "REDOWNLOAD SELECTED VERSION",
                scale,
                () => RedownloadPressed?.Invoke()
            );
        _clearCachedVersionsButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "CLEAR CACHE",
                scale,
                () => ClearCachedVersionsPressed?.Invoke(),
                "Old versions"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "CLEAR CACHED VERSIONS",
                scale,
                () => ClearCachedVersionsPressed?.Invoke()
            );
        _diagnosticsButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "DIAGNOSTICS",
                scale,
                () => DiagnosticsPressed?.Invoke(),
                "Export report"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "EXPORT DIAGNOSTICS",
                scale,
                () => DiagnosticsPressed?.Invoke()
            );
        _showLastErrorButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "LAST ERROR",
                scale,
                () => ShowLastErrorPressed?.Invoke(),
                "Open details"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "SHOW LAST ERROR",
                scale,
                () => ShowLastErrorPressed?.Invoke()
            );
        _copyRawLogButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "COPY LOG",
                scale,
                () => CopyRawLogPressed?.Invoke(),
                "Review first"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "COPY RAW LOG (REVIEW BEFORE SHARING)",
                scale,
                () => CopyRawLogPressed?.Invoke()
            );
    }

    private static VBoxContainer BuildActionGroup(float scale)
    {
        var group = new VBoxContainer();
        group.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        group.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SectionSeparation, scale)
        );
        return group;
    }

    private static GridContainer BuildCompactSupportToolsGrid(
        float scale,
        bool compact,
        bool compactStackedActionRows
    )
    {
        var grid = new GridContainer
        {
            Columns = compactStackedActionRows ? 1 : 2,
            Visible = compact,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(6, scale)
        );
        return grid;
    }

    private static Container BuildCompactCloudPrimaryActionsRow(
        Container parent,
        float scale,
        bool compactStackedActionRows
    )
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactCloudPrimaryActionSeparation, scale)
        );
        parent.AddChild(row);
        return row;
    }

    private static Container BuildCompactCloudOptionsRow(
        Container parent,
        float scale,
        bool compactStackedActionRows
    )
    {
        Container row = compactStackedActionRows ? new VBoxContainer() : new HBoxContainer();
        row.Visible = false;
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactCloudOptionToggleSeparation, scale)
        );
        parent.AddChild(row);
        return row;
    }

    private void ToggleSupportOptions()
    {
        _supportExpanded = !_supportExpanded;
        _supportGroup.Visible = _supportExpanded;
        SetCompactActionButtonText(_supportToggle, _supportExpanded
            ? (_compact ? CompactPlaySyncDrawerText("HIDE TOOLS", "Advanced fixes") : "HIDE SUPPORT OPTIONS")
            : SupportToggleText());
    }

    private string SupportToggleText()
        => _compact ? CompactPlaySyncDrawerText("RECOVERY / TOOLS", "Advanced fixes") : "MORE SUPPORT OPTIONS";

    private void MoveCompactCloudSafetyCueBeforeCloudActions()
    {
        if (!_compact)
            return;

        _cloudGroup.MoveChild(_cloudSafetyLabel, _pushPullRow.GetIndex());
        _cloudGroup.MoveChild(_cloudSafetyToggle, _cloudSafetyLabel.GetIndex());
    }

    private void ArmCloudPush()
    {
        if (CloudPushArmRequested?.Invoke() == false)
            return;

        _pushButton.Visible = false;
        _confirmPushButton.Visible = true;
        _pushConfirmationLabel.Visible = true;
    }

    private void ConfirmCloudPush()
    {
        ResetCloudPushArm();
        CloudPushPressed?.Invoke();
    }

    private void ResetCloudPushArm(bool showPushButton = true)
    {
        _confirmPushButton.Visible = false;
        _pushConfirmationLabel.Visible = false;
        ApplyCloudPushVisibility(showPushButton);
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
        ApplyBranchControlVisibility();
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
}
