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

        var supportFoundation = BuildSupportFoundation(scale, compact, _compactStackedActionRows);
        _supportGroup = supportFoundation.Group;
        var supportToolsParent = supportFoundation.ToolsParent;

        var primaryActions = BuildPrimaryActionControls(scale, compact, supportToolsParent);
        _retryButton = primaryActions.RetryButton;
        _launchButton = primaryActions.LaunchButton;
        _safeLaunchButton = primaryActions.SafeLaunchButton;

        var rendererControls = BuildRendererControls(scale, compact);
        _rendererGroup = rendererControls.Group;
        _rendererAutoButton = rendererControls.AutoButton;
        _rendererVulkanButton = rendererControls.VulkanButton;
        _rendererOpenGlButton = rendererControls.OpenGlButton;
        ApplyRendererMode(_rendererMode, notify: false);

        var branchControls = BuildBranchControls(scale, compact);
        _branchDetailsToggle = branchControls.DetailsToggle;
        _branchDropdown = branchControls.Dropdown;
        _branchHelpLabel = branchControls.HelpLabel;

        var readySummary = BuildReadyVersionSummaryControls(scale, compact);
        _readyVersionSummaryPanel = readySummary.Panel;
        _readyVersionSummaryLabel = readySummary.Label;

        SetGameBranch(_gameBranch);

        var cloudControls = BuildCloudControls(scale, compact);
        _cloudGroup = cloudControls.Group;
        _pushPullRow = cloudControls.PushPullRow;
        _pullButton = cloudControls.PullButton;
        _cloudPushToggle = cloudControls.CloudPushToggle;
        _pushButton = cloudControls.PushButton;
        _confirmPushButton = cloudControls.ConfirmPushButton;
        _cancelCloudOperationButton =
            cloudControls.CancelOperationButton;
        _cloudOperationProgressGroup = cloudControls.OperationProgressGroup;
        _cloudOperationPhaseLabel = cloudControls.OperationPhaseLabel;
        _cloudOperationDetailLabel = cloudControls.OperationDetailLabel;
        _cloudOperationProgressBar = cloudControls.OperationProgressBar;
        _cloudPushEligibilityLabel = cloudControls.PushEligibilityLabel;
        _pushConfirmationLabel = cloudControls.PushConfirmationLabel;
        _cloudSafetyLabel = cloudControls.CloudSafetyLabel;
        _cloudSafetyToggle = cloudControls.CloudSafetyToggle;
        _cloudOptionsToggle = cloudControls.CloudOptionsToggle;
        _compactCloudOptionsRow = cloudControls.CompactCloudOptionsRow;
        _localBackupToggle = cloudControls.LocalBackupToggle;
        _cloudSyncToggle = cloudControls.CloudSyncToggle;

        var saveRecoveryControls = BuildSaveRecoveryControls(scale, compact);
        _saveRecoveryGroup = saveRecoveryControls.Group;
        _saveRecoveryScanButton = saveRecoveryControls.ScanButton;
        _saveRecoveryCurrentExportButton =
            saveRecoveryControls.CurrentExportButton;
        _saveRecoveryCandidateDropdown =
            saveRecoveryControls.CandidateDropdown;
        _saveRecoveryExportButton = saveRecoveryControls.ExportButton;
        _saveRecoveryRestoreButton = saveRecoveryControls.RestoreButton;
        _saveRecoveryUndoButton = saveRecoveryControls.UndoButton;
        _saveRecoveryApproveButton = saveRecoveryControls.ApproveButton;
        _saveRecoveryStatusLabel = saveRecoveryControls.StatusLabel;

        ConfigureLocalBackupToggle();
        ConfigureCloudSyncToggle();
        UpdateBranchHelpText();

        var modsControls = BuildModsControls(scale, compact);
        _modsGroup = modsControls.Group;
        _playVanillaButton = modsControls.PlayVanillaButton;
        _playModdedButton = modsControls.PlayModdedButton;
        _modsStatusLabel = modsControls.StatusLabel;
        _modsList = modsControls.ModsList;
        _workshopSyncButton = modsControls.WorkshopSyncButton;
        _workshopClearButton = modsControls.WorkshopClearButton;

        var supportControls = BuildSupportControls(scale, compact, supportToolsParent);
        _supportToggle = supportControls.SupportToggle;
        _updateButton = supportControls.UpdateButton;
        _refreshVersionsButton = supportControls.RefreshVersionsButton;
        _redownloadButton = supportControls.RedownloadButton;
        _clearCachedVersionsButton = supportControls.ClearCachedVersionsButton;
        _diagnosticsButton = supportControls.DiagnosticsButton;
        _showLastErrorButton = supportControls.ShowLastErrorButton;
        _copyRawLogButton = supportControls.CopyRawLogButton;

        BuildDestinationLayout();
    }
}
