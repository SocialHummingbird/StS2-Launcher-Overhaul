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
            compact,
            "Play safely"
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
            compact ? CompactRetryButtonText() : "Retry",
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            () => RetryPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(_retryButton, scale);
        SetCompactActionButtonText(_retryButton, _retryButton.Text);

        _launchButton = AddPrimaryHiddenButton(
            this,
            "Start Game",
            scale,
            () => LaunchPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(_launchButton, scale);
        _safeLaunchButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke(),
                "Cloud off"
            )
            : AddSecondaryHiddenButton(
                this,
                "Safe Start",
                scale,
                () => SafeLaunchPressed?.Invoke()
            );
        LauncherButtonStyles.ApplySafeAction(_safeLaunchButton, scale);

        _branchDetailsToggle = new StyledButton(
            "Show Version Details",
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

        _readyVersionSummaryPanel = new Button
        {
            Text = "",
            ClipText = true,
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "Open save safety check",
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
        ApplyReadyVersionSummaryButtonStyle(_readyVersionSummaryPanel, scale, compact);
        _readyVersionSummaryPanel.Pressed += OpenCompactCloudSafetyFromReadySummary;
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
        _readyVersionSummaryLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _readyVersionSummaryLabel.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        _readyVersionSummaryLabel.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryHorizontalMargin,
            scale
        );
        _readyVersionSummaryLabel.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
        _readyVersionSummaryLabel.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(
            LauncherSectionMetrics.CompactVersionSummaryVerticalMargin,
            scale
        );
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
            compact ? CompactCloudPushToggleText(expanded: false) : "Push Locked",
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
            "Show Save Settings",
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
            ? AddCompactSupportToolButton(cloudOptionsParent, "Save Backup Off", scale, null)
            : AddSecondaryHiddenButton(_cloudGroup, "Local Backup: Off", scale, null);
        _cloudSyncToggle = compact
            ? AddCompactSupportToolButton(cloudOptionsParent, "Cloud Sync Off", scale, null)
            : AddSecondaryHiddenButton(_cloudGroup, "Game Cloud Sync: Off", scale, null);
        ConfigureLocalBackupToggle();
        ConfigureCloudSyncToggle();
        UpdateBranchHelpText();
        ArrangeCompactCloudGroupPriority();

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
                "Check Files",
                scale,
                () => CheckForUpdatesPressed?.Invoke(),
                "Updates"
            )
            : AddPrimaryHiddenButton(
                _supportGroup,
                "Check for Updates",
                scale,
                () => CheckForUpdatesPressed?.Invoke()
            );
        LauncherButtonStyles.ApplySupportAction(_updateButton, scale);
        _refreshVersionsButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Game Versions",
                scale,
                () => RefreshGameVersionsPressed?.Invoke(),
                "Refresh list"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Refresh Game Versions",
                scale,
                () => RefreshGameVersionsPressed?.Invoke()
            );
        _redownloadButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Repair Files",
                scale,
                () => RedownloadPressed?.Invoke(),
                "Rebuild game"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Redownload Selected Version",
                scale,
                () => RedownloadPressed?.Invoke()
            );
        _clearCachedVersionsButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Free Space",
                scale,
                () => ClearCachedVersionsPressed?.Invoke(),
                "Old versions"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Clear Cached Versions",
                scale,
                () => ClearCachedVersionsPressed?.Invoke()
            );
        _diagnosticsButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Help Report",
                scale,
                () => DiagnosticsPressed?.Invoke(),
                "Share details"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Create Help Report",
                scale,
                () => DiagnosticsPressed?.Invoke()
            );
        _showLastErrorButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Last Problem",
                scale,
                () => ShowLastErrorPressed?.Invoke(),
                "Open details"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Show Last Problem",
                scale,
                () => ShowLastErrorPressed?.Invoke()
            );
        _copyRawLogButton = compact
            ? AddCompactSupportToolButton(
                supportToolsParent,
                "Copy Log",
                scale,
                () => CopyRawLogPressed?.Invoke(),
                "Review first"
            )
            : AddSecondaryHiddenButton(
                _supportGroup,
                "Copy Launcher Log (Review First)",
                scale,
                () => CopyRawLogPressed?.Invoke()
            );

        ArrangeCompactReadyStatePriority();
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
            ? (_compact ? CompactPlaySyncDrawerText("Hide Fixes", "Back to play") : "Hide Support Options")
            : SupportToggleText());
    }

    private string SupportToggleText()
        => _compact ? CompactPlaySyncDrawerText("Fixes & Help", "Repair tools") : "More Support Options";

    private void MoveCompactCloudSafetyCueBeforeCloudActions()
    {
        if (!_compact)
            return;

        _cloudGroup.MoveChild(_cloudSafetyToggle, 0);
        MoveChildAfter(_cloudGroup, _cloudSafetyLabel, _cloudSafetyToggle);
        MoveChildAfter(_cloudGroup, _pushPullRow, _cloudSafetyLabel);
    }

    private void ArrangeCompactCloudGroupPriority()
    {
        if (!_compact)
            return;

        var launchParent = _launchButton.GetParent();
        if (launchParent != _cloudGroup)
        {
            launchParent?.RemoveChild(_launchButton);
            _cloudGroup.AddChild(_launchButton);
        }

        MoveCompactCloudSafetyCueBeforeCloudActions();
        MoveChildAfter(_cloudGroup, _launchButton, _pushPullRow);
        MoveChildAfter(_cloudGroup, _cloudOptionsToggle, _launchButton);
        if (_compactCloudOptionsRow != null)
            MoveChildAfter(_cloudGroup, _compactCloudOptionsRow, _cloudOptionsToggle);
    }

    private void ArrangeCompactReadyStatePriority()
    {
        if (!_compact)
            return;

        var readyPrimaryPath = _launchButton.GetParent() == _cloudGroup
            ? _cloudGroup
            : (Control)_launchButton;
        MoveChild(_readyVersionSummaryPanel, _branchDetailsToggle.GetIndex());
        MoveAfter(_branchDetailsToggle, readyPrimaryPath);
        MoveAfter(_branchDropdown, _branchDetailsToggle);
        MoveAfter(_branchHelpLabel, _branchDropdown);
    }

    private void MoveAfter(Control child, Control previous)
    {
        var previousIndex = previous.GetIndex();
        var targetIndex = child.GetIndex() < previousIndex
            ? previousIndex
            : previousIndex + 1;
        MoveChild(child, Math.Min(targetIndex, GetChildCount() - 1));
    }

    private static void MoveChildAfter(Node parent, Node child, Node previous)
    {
        var previousIndex = previous.GetIndex();
        var targetIndex = child.GetIndex() < previousIndex
            ? previousIndex
            : previousIndex + 1;
        parent.MoveChild(child, Math.Min(targetIndex, parent.GetChildCount() - 1));
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
