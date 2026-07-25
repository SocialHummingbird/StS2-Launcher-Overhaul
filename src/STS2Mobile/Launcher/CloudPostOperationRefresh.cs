#nullable enable

using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly record struct CloudPostOperationSnapshot(
    int ImportantLocalSaveEvidenceCount,
    int CurrentMirrorSaveCount,
    CloudPushEligibilityResult UploadEligibility
);

internal readonly record struct CloudPostOperationResolution(
    ManualCloudSyncResult Result,
    bool CompletionEvidenceRequired,
    bool CompletionEvidenceRecorded,
    CloudPostOperationSnapshot Snapshot
);

internal static class CloudPostOperationRefresh
{
    internal static CloudPostOperationResolution ResolveCompletion(
        ManualCloudSyncResult result,
        bool evidenceRequired,
        Func<bool>? recordCompletionEvidence,
        Func<CloudPostOperationSnapshot> captureCurrentState,
        Action<ManualCloudSyncResult>? recordIncompleteResult = null
    )
    {
        ArgumentNullException.ThrowIfNull(captureCurrentState);

        var evidenceRecorded = !evidenceRequired;
        if (evidenceRequired && result.CanRecordCompletionEvidence)
        {
            if (recordCompletionEvidence != null)
                evidenceRecorded = recordCompletionEvidence();
        }
        else if (evidenceRequired)
        {
            recordIncompleteResult?.Invoke(result);
        }

        var snapshot = captureCurrentState();
        return new CloudPostOperationResolution(
            result,
            evidenceRequired,
            evidenceRecorded,
            snapshot
        );
    }
}
