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
    CloudPostOperationSnapshot Snapshot
);

internal static class CloudPostOperationRefresh
{
    internal static CloudPostOperationResolution ResolveCompletion(
        ManualCloudSyncResult result,
        Func<CloudPostOperationSnapshot> captureCurrentState
    )
    {
        ArgumentNullException.ThrowIfNull(captureCurrentState);

        var snapshot = captureCurrentState();
        return new CloudPostOperationResolution(
            result,
            snapshot
        );
    }
}
