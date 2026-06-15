using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal ActionSection(float scale, bool compact = false)
    {
        _compact = compact;
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

        _retryButton = AddHiddenButton(
            this,
            "RETRY",
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            () => RetryPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(_retryButton, scale);

        _launchButton = AddPrimaryHiddenButton(
            this,
            "START GAME",
            scale,
            () => LaunchPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(_launchButton, scale);
        _safeLaunchButton = AddSecondaryHiddenButton(
            this,
            "SAFE START",
            scale,
            () => SafeLaunchPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySafeAction(_safeLaunchButton, scale);

        _branchDropdown = new OptionButton();
        _branchDropdown.Visible = false;
        _branchDropdown.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _branchDropdown.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SecondaryButtonHeight, scale)
        );
        _branchDropdown.ItemSelected += ApplyGameBranch;
        AddChild(_branchDropdown);

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
        _branchHelpLabel.Visible = false;
        AddChild(_branchHelpLabel);

        _branchDetailsToggle = new StyledButton(
            "SHOW VERSION DETAILS",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            height: LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_branchDetailsToggle, scale);
        _branchDetailsToggle.Visible = false;
        _branchDetailsToggle.Pressed += ToggleBranchDetails;
        AddChild(_branchDetailsToggle);
        SetGameBranch(_gameBranch);

        _cloudGroup = BuildActionGroup(scale);
        _cloudGroup.Visible = false;
        AddChild(_cloudGroup);

        _pushPullRow = new VBoxContainer();
        _pushPullRow.Visible = false;
        _pushPullRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.PushPullRowSeparation, scale)
        );
        _pullButton = AddPushPullButton(
            _pushPullRow,
            compact ? "Pull Saves" : "Pull Saves from Steam Cloud",
            scale,
            () =>
            {
                ResetCloudPushArm();
                CloudPullPressed?.Invoke();
            }
        );
        _pushButton = AddPushPullButton(
            _pushPullRow,
            compact ? "Push Saves" : PushButtonText,
            scale,
            ArmCloudPush
        );
        _confirmPushButton = AddPushPullButton(
            _pushPullRow,
            compact ? "Confirm Cloud Overwrite" : PushConfirmButtonText,
            scale,
            ConfirmCloudPush
        );
        _confirmPushButton.Visible = false;
        LauncherButtonStyles.ApplyPrimaryAction(_confirmPushButton, scale);
        _pushConfirmationLabel = new StyledLabel(
            compact
                ? "Push will overwrite Steam Cloud saves for this version. Confirm only after Pull/local saves are verified."
                : "Confirming Push uploads Android saves to Steam Cloud for the selected version and can overwrite remote Steam Cloud saves. Continue only after Pull and local save evidence are verified.",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        _pushConfirmationLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
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
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        _cloudSafetyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cloudSafetyLabel.MouseFilter = MouseFilterEnum.Ignore;
        _cloudSafetyLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.OrangeHot
        );
        _cloudGroup.AddChild(_cloudSafetyLabel);

        _cloudSafetyToggle = new StyledButton(
            "SHOW CLOUD SAFETY",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            height: LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_cloudSafetyToggle, scale);
        _cloudSafetyToggle.Visible = compact;
        _cloudSafetyToggle.Pressed += ToggleCloudSafety;
        _cloudGroup.AddChild(_cloudSafetyToggle);

        _cloudOptionsToggle = new StyledButton(
            "SHOW CLOUD OPTIONS",
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            height: LauncherSectionMetrics.SecondaryButtonHeight
        );
        LauncherButtonStyles.ApplySupportAction(_cloudOptionsToggle, scale);
        _cloudOptionsToggle.Visible = compact;
        _cloudOptionsToggle.Pressed += ToggleCloudOptions;
        _cloudGroup.AddChild(_cloudOptionsToggle);

        _localBackupToggle = AddSecondaryHiddenButton(_cloudGroup, "Local Backup: OFF", scale, null);
        _cloudSyncToggle = AddSecondaryHiddenButton(_cloudGroup, "Game Cloud Sync: OFF", scale, null);
        ConfigureLocalBackupToggle();
        ConfigureCloudSyncToggle();
        UpdateBranchHelpText();

        _supportToggle = AddSecondaryHiddenButton(
            this,
            compact ? "SUPPORT OPTIONS" : "MORE SUPPORT OPTIONS",
            scale,
            ToggleSupportOptions
        );
        LauncherButtonStyles.ApplySupportAction(_supportToggle, scale);

        _supportGroup = BuildActionGroup(scale);
        _supportGroup.Visible = false;
        AddChild(_supportGroup);

        _updateButton = AddPrimaryHiddenButton(
            _supportGroup,
            compact ? "CHECK UPDATES" : "CHECK FOR UPDATES",
            scale,
            () => CheckForUpdatesPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(_updateButton, scale);
        _refreshVersionsButton = AddSecondaryHiddenButton(
            _supportGroup,
            compact ? "REFRESH VERSIONS" : "REFRESH GAME VERSIONS",
            scale,
            () => RefreshGameVersionsPressed?.Invoke()
        );
        _redownloadButton = AddSecondaryHiddenButton(
            _supportGroup,
            compact ? "REDOWNLOAD VERSION" : "REDOWNLOAD SELECTED VERSION",
            scale,
            () => RedownloadPressed?.Invoke()
        );
        _clearCachedVersionsButton = AddSecondaryHiddenButton(
            _supportGroup,
            compact ? "CLEAR VERSION CACHE" : "CLEAR CACHED VERSIONS",
            scale,
            () => ClearCachedVersionsPressed?.Invoke()
        );
        _diagnosticsButton = AddSecondaryHiddenButton(
            _supportGroup,
            "EXPORT DIAGNOSTICS",
            scale,
            () => DiagnosticsPressed?.Invoke()
        );
        _showLastErrorButton = AddSecondaryHiddenButton(
            _supportGroup,
            "SHOW LAST ERROR",
            scale,
            () => ShowLastErrorPressed?.Invoke()
        );
        _copyRawLogButton = AddSecondaryHiddenButton(
            _supportGroup,
            compact ? "COPY RAW LOG" : "COPY RAW LOG (REVIEW BEFORE SHARING)",
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

    private void ToggleSupportOptions()
    {
        _supportExpanded = !_supportExpanded;
        _supportGroup.Visible = _supportExpanded;
        _supportToggle.Text = _supportExpanded
            ? "HIDE SUPPORT OPTIONS"
            : (_compact ? "SUPPORT OPTIONS" : "MORE SUPPORT OPTIONS");
    }

    private void ArmCloudPush()
    {
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
        _pushButton.Visible = showPushButton;
        _confirmPushButton.Visible = false;
        _pushConfirmationLabel.Visible = false;
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
}
