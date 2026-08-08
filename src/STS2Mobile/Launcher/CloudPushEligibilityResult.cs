using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace STS2Mobile.Launcher;

internal enum CloudPushEligibilityBlockCode
{
    ImportantLocalSavesMissing,
    IncompletePullRequiresRecovery,
    LocalRecoveryRequiresValidation,
}

internal enum CloudPushRequiredActionCode
{
    VerifyAndroidLocalSaves,
    RecoverIncompletePull,
    ValidateAndApproveRecoveredSaves,
}

internal readonly record struct CloudPushRequiredAction(
    CloudPushRequiredActionCode Code,
    string Description
);

internal readonly record struct CloudPushEligibilityBlock(
    CloudPushEligibilityBlockCode Code,
    string Reason,
    CloudPushRequiredAction RequiredNextAction
);

internal sealed class CloudPushEligibilityResult
{
    internal CloudPushEligibilityResult(
        IEnumerable<CloudPushEligibilityBlock> blockingReasons
    )
    {
        var blocks = new List<CloudPushEligibilityBlock>(blockingReasons);
        var actions = new List<CloudPushRequiredAction>();
        var actionCodes = new HashSet<CloudPushRequiredActionCode>();
        foreach (var block in blocks)
        {
            if (actionCodes.Add(block.RequiredNextAction.Code))
                actions.Add(block.RequiredNextAction);
        }

        BlockingReasons = new ReadOnlyCollection<CloudPushEligibilityBlock>(blocks);
        RequiredNextActions = new ReadOnlyCollection<CloudPushRequiredAction>(actions);
    }

    internal bool IsEligible => BlockingReasons.Count == 0;
    internal IReadOnlyList<CloudPushEligibilityBlock> BlockingReasons { get; }
    internal IReadOnlyList<CloudPushRequiredAction> RequiredNextActions { get; }
}
