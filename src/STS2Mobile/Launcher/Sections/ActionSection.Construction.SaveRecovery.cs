#nullable enable

using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly record struct SaveRecoveryControls(
        VBoxContainer Group,
        Button ScanButton,
        Button CurrentExportButton,
        OptionButton CandidateDropdown,
        Button ExportButton,
        Button RestoreButton,
        Button UndoButton,
        Button ApproveButton,
        Label StatusLabel
    );

    private SaveRecoveryControls BuildSaveRecoveryControls(
        float scale,
        bool compact
    )
    {
        var group = BuildActionGroup(scale);
        group.Visible = true;

        var heading = new StyledLabel(
            "Local Save Recovery",
            scale,
            fontSize: compact ? 16 : 18,
            align: HorizontalAlignment.Left
        );
        heading.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        group.AddChild(heading);

        var recoveryActions = BuildSaveRecoveryActionRow(group, scale);
        var scanButton = AddPushPullButton(
            recoveryActions,
            "Scan for Recovery Copies",
            scale,
            () => SaveRecoveryScanPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(scanButton, scale);
        scanButton.AccessibilityDescription =
            "Scan known local backup and snapshot locations without contacting Steam.";

        var currentExportButton = AddPushPullButton(
            recoveryActions,
            "Export Current Android Saves",
            scale,
            () => SaveRecoveryCurrentExportPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(currentExportButton, scale);
        currentExportButton.AccessibilityDescription =
            "Export the current selected Android save context byte-for-byte without contacting Steam.";

        var undoButton = AddPushPullButton(
            recoveryActions,
            "Undo Last Restore",
            scale,
            () => SaveRecoveryUndoPressed?.Invoke()
        );
        LauncherButtonStyles.ApplySupportAction(undoButton, scale);
        undoButton.AccessibilityDescription =
            "Restore the local save state captured immediately before the last restore.";
        undoButton.Visible = false;

        var approveButton = AddPushPullButton(
            recoveryActions,
            "Approve Validated Save for Sync",
            scale,
            () => SaveRecoveryApprovePressed?.Invoke()
        );
        LauncherButtonStyles.ApplySafeAction(approveButton, scale);
        approveButton.AccessibilityDescription =
            "Approve a separately validated restored save for later synchronization.";
        approveButton.Visible = false;

        var candidateActions = BuildSaveRecoveryActionRow(group, scale);
        var candidateDropdown = new OptionButton
        {
            AccessibilityName = "Recovery copy",
            AccessibilityDescription =
                "Select a local recovery copy and review its provenance before restoring.",
            ClipText = true,
            Disabled = true,
            FitToLongestItem = !compact,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(
                    compact
                        ? LauncherSectionMetrics.PrimaryButtonHeight
                        : LauncherSectionMetrics.SecondaryButtonHeight,
                    scale
                )
            ),
        };
        LauncherButtonStyles.ApplyDropdownAction(
            candidateDropdown,
            scale,
            compact
                ? LauncherSectionMetrics.PrimaryButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            compact
        );
        candidateDropdown.AddItem("No recovery copies scanned");
        candidateDropdown.ItemSelected += SelectSaveRecoveryCandidate;
        candidateActions.AddChild(candidateDropdown);

        var exportButton = AddPushPullButton(
            candidateActions,
            "Export Recovery Bundle",
            scale,
            RequestSelectedSaveRecoveryExport
        );
        LauncherButtonStyles.ApplySupportAction(exportButton, scale);
        exportButton.AccessibilityDescription =
            "Export the selected recovery copy for safekeeping or support review.";

        var restoreButton = AddPushPullButton(
            candidateActions,
            "Restore on Android",
            scale,
            RequestSelectedSaveRecoveryRestore
        );
        LauncherButtonStyles.ApplyCloudPullAction(restoreButton, scale);
        restoreButton.AccessibilityDescription =
            "Request a local Android restore. This control does not upload to Steam.";

        var statusLabel = new StyledLabel(
            "No recovery copies scanned. Recovery actions use local files only; Steam is not changed.",
            scale,
            fontSize: compact
                ? LauncherSectionMetrics.CompactVersionSummaryFontSize
                : LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        statusLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        group.AddChild(statusLabel);

        exportButton.Disabled = true;
        restoreButton.Disabled = true;
        AddChild(group);

        return new SaveRecoveryControls(
            group,
            scanButton,
            currentExportButton,
            candidateDropdown,
            exportButton,
            restoreButton,
            undoButton,
            approveButton,
            statusLabel
        );
    }

    private Container BuildSaveRecoveryActionRow(
        Container parent,
        float scale
    )
    {
        Container row = _compactStackedActionRows
            ? new VBoxContainer()
            : new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                CompactCloudPrimaryActionSeparation,
                scale
            )
        );
        parent.AddChild(row);
        return row;
    }
}
