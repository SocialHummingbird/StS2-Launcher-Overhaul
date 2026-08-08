using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static class CloudPushEligibilityPolicy
{
    internal static CloudPushEligibilityResult Evaluate(
        CloudPushEligibilityState state
    )
    {
        var blocks = new List<CloudPushEligibilityBlock>();

        if (state.HasRecoverySyncHold)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.LocalRecoveryRequiresValidation,
                "Upload is blocked because recovered saves are still Android-only.",
                CloudPushRequiredActionCode.ValidateAndApproveRecoveredSaves,
                "Open the recovered save locally, verify it, then explicitly approve it for sync."
            );
        }

        if (state.HasIncompletePull)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.IncompletePullRequiresRecovery,
                "Upload is blocked because a previous Pull did not complete.",
                CloudPushRequiredActionCode.RecoverIncompletePull,
                "Retry and complete the interrupted Pull before uploading."
            );
        }

        if (!state.HasImportantLocalSaveEvidence)
        {
            AddBlock(
                blocks,
                CloudPushEligibilityBlockCode.ImportantLocalSavesMissing,
                "No transferable Android local save files were found.",
                CloudPushRequiredActionCode.VerifyAndroidLocalSaves,
                "Open the game and verify that Android local saves exist."
            );
        }

        return new CloudPushEligibilityResult(blocks);
    }

    private static void AddBlock(
        ICollection<CloudPushEligibilityBlock> blocks,
        CloudPushEligibilityBlockCode blockCode,
        string reason,
        CloudPushRequiredActionCode actionCode,
        string action
    )
        => blocks.Add(
            new CloudPushEligibilityBlock(
                blockCode,
                reason,
                new CloudPushRequiredAction(actionCode, action)
            )
        );
}
