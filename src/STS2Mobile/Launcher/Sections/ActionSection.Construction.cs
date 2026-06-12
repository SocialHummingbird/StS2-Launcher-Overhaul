using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    internal ActionSection(float scale)
    {
        AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.SectionSeparation, scale)
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
            "LAUNCH",
            scale,
            () => LaunchPressed?.Invoke()
        );
        LauncherButtonStyles.ApplyPrimaryAction(_launchButton, scale);
        _safeLaunchButton = AddSecondaryHiddenButton(
            this,
            "SAFE LAUNCH",
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
        SetGameBranch(_gameBranch);

        _cloudGroup = BuildActionGroup(scale);
        _cloudGroup.Visible = false;
        AddChild(_cloudGroup);

        _localBackupToggle = AddSecondaryHiddenButton(_cloudGroup, "Local Backup: OFF", scale, null);
        _cloudSyncToggle = AddSecondaryHiddenButton(_cloudGroup, "Game Cloud Sync: OFF", scale, null);
        ConfigureLocalBackupToggle();
        ConfigureCloudSyncToggle();

        _pushPullRow = new VBoxContainer();
        _pushPullRow.Visible = false;
        _pushPullRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.PushPullRowSeparation, scale)
        );
        _pushButton = AddPushPullButton(
            _pushPullRow,
            PushButtonText,
            scale,
            ArmCloudPush
        );
        _confirmPushButton = AddPushPullButton(
            _pushPullRow,
            PushConfirmButtonText,
            scale,
            ConfirmCloudPush
        );
        _confirmPushButton.Visible = false;
        LauncherButtonStyles.ApplyPrimaryAction(_confirmPushButton, scale);
        _pullButton = AddPushPullButton(
            _pushPullRow,
            "Pull from Cloud",
            scale,
            () =>
            {
                ResetCloudPushArm();
                CloudPullPressed?.Invoke();
            }
        );
        _cloudGroup.AddChild(_pushPullRow);

        _supportToggle = AddSecondaryHiddenButton(
            this,
            "MORE SUPPORT OPTIONS",
            scale,
            ToggleSupportOptions
        );
        LauncherButtonStyles.ApplySupportAction(_supportToggle, scale);

        _supportGroup = BuildActionGroup(scale);
        _supportGroup.Visible = false;
        AddChild(_supportGroup);

        _updateButton = AddPrimaryHiddenButton(
            _supportGroup,
            "CHECK FOR UPDATES",
            scale,
            () => CheckForUpdatesPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(_updateButton, scale);
        _refreshVersionsButton = AddSecondaryHiddenButton(
            _supportGroup,
            "REFRESH GAME VERSIONS",
            scale,
            () => RefreshGameVersionsPressed?.Invoke()
        );
        _redownloadButton = AddSecondaryHiddenButton(
            _supportGroup,
            "REDOWNLOAD SELECTED VERSION",
            scale,
            () => RedownloadPressed?.Invoke()
        );
        _clearCachedVersionsButton = AddSecondaryHiddenButton(
            _supportGroup,
            "CLEAR CACHED VERSIONS",
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
            "COPY RAW ERROR LOG",
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
        _supportToggle.Text = _supportExpanded ? "HIDE SUPPORT OPTIONS" : "MORE SUPPORT OPTIONS";
    }

    private void ArmCloudPush()
    {
        _pushButton.Visible = false;
        _confirmPushButton.Visible = true;
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
