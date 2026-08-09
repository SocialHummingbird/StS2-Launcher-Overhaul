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
            "Play",
            "Launch the game, choose a version, and manage mods.",
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

        var homeJourney = BuildHomeJourney(scale, compact);
        _homeJourney = homeJourney.Group;
        _homeAccountState = homeJourney.AccountState;
        _homeGameState = homeJourney.GameState;
        _homeSaveState = homeJourney.SaveState;

        var branchControls = BuildBranchControls(scale, compact);
        _branchDetailsToggle = branchControls.DetailsToggle;
        _branchDropdown = branchControls.Dropdown;
        _branchHelpLabel = branchControls.HelpLabel;

        var readySummary = BuildReadyVersionSummaryControls(scale, compact);
        _readyVersionSummaryPanel = readySummary.Panel;
        _readyVersionSummaryLabel = readySummary.Label;

        var saveSyncControls = BuildSaveSyncControls(scale, compact);
        _saveSyncGroup = saveSyncControls.Group;
        _saveSyncNowButton = saveSyncControls.SyncNowButton;
        _savePullButton = saveSyncControls.PullButton;
        _savePushButton = saveSyncControls.PushButton;
        _saveSyncStatus = saveSyncControls.Status;
        _saveLastSuccessState = saveSyncControls.LastSuccessState;
        _saveLocalState = saveSyncControls.LocalState;
        _saveSteamState = saveSyncControls.SteamState;

        SetGameBranch(_gameBranch);

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
