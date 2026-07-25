using System;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    internal void CloudPushPressed()
    {
        if (RejectWhenOperationActive())
            return;

        var pushContext = CloudPushSafetyContext.Create(_model.DataDir);
        if (!EvaluateCloudPushEligibility(pushContext).IsEligible)
            return;

        var progress = new Steam.CloudOperationProgressTracker(
            Steam.CloudOperationKind.Push,
            ReportCloudOperationState
        );
        progress.Preparing("Waiting for Upload confirmation");
        RequestCloudSync(ManualCloudSyncRequest.Push(
            pushContext.DataDir,
            pushContext.SelectedBranch,
            progress
        ));
    }

    internal CloudPushEligibilityResult EvaluateCloudPushEligibility()
        => EvaluateCloudPushEligibility(
            CloudPushSafetyContext.Create(_model.DataDir)
        );

    private static CloudPushEligibilityResult EvaluateCloudPushEligibility(
        CloudPushSafetyContext pushContext
    )
        => CloudPushEligibilityPolicy.Evaluate(
            pushContext.CaptureEligibilityState()
        );

    private static CloudPushEligibilityResult EvaluateCloudPushEligibility(
        CloudPushSafetyContext pushContext,
        bool hasImportantLocalSaveEvidence
    )
        => CloudPushEligibilityPolicy.Evaluate(
            pushContext.CaptureEligibilityState(
                hasImportantLocalSaveEvidence
            )
        );

    private static bool EnsureCloudPushStillEligible(
        string dataDir,
        string expectedBranch
    )
    {
        var currentBranch = LauncherPreferences.ReadGameBranch();
        if (
            !string.Equals(
                SteamGameBranch.Normalize(expectedBranch),
                SteamGameBranch.Normalize(currentBranch),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new InvalidOperationException(
                "Upload blocked before any Steam Cloud write: "
                    + $"the selected game version changed from "
                    + $"{SteamGameBranch.DisplayName(expectedBranch)} to "
                    + $"{SteamGameBranch.DisplayName(currentBranch)} after confirmation."
            );
        }

        var eligibility = EvaluateCloudPushEligibility(
            CloudPushSafetyContext.Create(dataDir)
        );
        if (eligibility.IsEligible)
            return true;

        var reasons = new StringBuilder();
        foreach (var block in eligibility.BlockingReasons)
        {
            if (reasons.Length > 0)
                reasons.Append(' ');

            reasons.Append(block.Reason);
        }

        throw new InvalidOperationException(
            "Upload blocked before any Steam Cloud write because its safety "
                + $"state changed after confirmation. {reasons}"
        );
    }
}
