using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudUploadEligibilityTest
{
    private static int _passed;

    private static int Main()
    {
        Run("eligible state has no blockers or actions", EligibleStatePasses);
        Run("each blocker is independently represented", EachBlockerIsRepresented);
        Run("all applicable blockers are accumulated", AllApplicableBlocksAccumulate);
        Run("required actions are deduplicated", RequiredActionsAreDeduplicated);
        Run("branch-only facts are ignored without a switch", BranchFactsAreConditional);
        Run("missing Pull suppresses a meaningless mismatch", MissingPullIsCanonical);
        Run("selected version appears in guidance", SelectedVersionAppearsInGuidance);
        Run("result collections are immutable", ResultCollectionsAreImmutable);

        AssertEqual("policy tests passed", 8, _passed);
        Console.WriteLine("Launcher cloud Upload eligibility tests passed 8/8.");
        return 0;
    }

    private static void EligibleStatePasses()
    {
        var result = Evaluate(EligibleState());
        AssertTrue("eligible result", result.IsEligible);
        AssertEqual("eligible blocker count", 0, result.BlockingReasons.Count);
        AssertEqual("eligible action count", 0, result.RequiredNextActions.Count);
    }

    private static void EachBlockerIsRepresented()
    {
        var cases = new (CloudPushEligibilityState State, CloudPushEligibilityBlockCode Code)[]
        {
            (
                EligibleState() with { SelectedModCount = 2 },
                CloudPushEligibilityBlockCode.ModsSelected
            ),
            (
                EligibleState() with { ManualPullCompleted = false },
                CloudPushEligibilityBlockCode.ManualPullNotCompleted
            ),
            (
                EligibleState() with { ManualPullMatchesSelectedVersion = false },
                CloudPushEligibilityBlockCode.ManualPullVersionMismatch
            ),
            (
                EligibleState() with { HasImportantLocalSaveEvidence = false },
                CloudPushEligibilityBlockCode.ImportantLocalSavesMissing
            ),
            (
                EligibleState() with { LocalSaveOriginMatchesSelectedRuntime = false },
                CloudPushEligibilityBlockCode.LocalSaveOriginNotVerified
            ),
            (
                SwitchedState() with { BranchSwitchEvidenceValid = false },
                CloudPushEligibilityBlockCode.BranchSwitchEvidenceInvalid
            ),
            (
                SwitchedState() with { HasManualPullAfterBranchSwitch = false },
                CloudPushEligibilityBlockCode.ManualPullAfterBranchSwitchMissing
            ),
            (
                SwitchedState() with { IsLocalBackupEnabled = false },
                CloudPushEligibilityBlockCode.LocalBackupDisabledAfterBranchSwitch
            ),
            (
                SwitchedState() with { HasBackupStoragePermission = false },
                CloudPushEligibilityBlockCode.BackupStoragePermissionMissing
            )
        };

        foreach (var testCase in cases)
        {
            var result = Evaluate(testCase.State);
            AssertFalse($"{testCase.Code} eligibility", result.IsEligible);
            AssertEqual($"{testCase.Code} blocker count", 1, result.BlockingReasons.Count);
            AssertEqual(
                $"{testCase.Code} code",
                testCase.Code,
                result.BlockingReasons[0].Code
            );
            AssertFalse(
                $"{testCase.Code} reason is empty",
                string.IsNullOrWhiteSpace(result.BlockingReasons[0].Reason)
            );
            AssertEqual(
                $"{testCase.Code} action count",
                1,
                result.RequiredNextActions.Count
            );
        }
    }

    private static void AllApplicableBlocksAccumulate()
    {
        var result = Evaluate(
            SwitchedState() with
            {
                SelectedModCount = 3,
                ManualPullMatchesSelectedVersion = false,
                HasImportantLocalSaveEvidence = false,
                LocalSaveOriginMatchesSelectedRuntime = false,
                BranchSwitchEvidenceValid = false,
                HasManualPullAfterBranchSwitch = false,
                IsLocalBackupEnabled = false,
                HasBackupStoragePermission = false
            }
        );

        AssertSequence(
            "complete blocker sequence",
            new[]
            {
                CloudPushEligibilityBlockCode.ModsSelected,
                CloudPushEligibilityBlockCode.ManualPullVersionMismatch,
                CloudPushEligibilityBlockCode.ImportantLocalSavesMissing,
                CloudPushEligibilityBlockCode.LocalSaveOriginNotVerified,
                CloudPushEligibilityBlockCode.BranchSwitchEvidenceInvalid,
                CloudPushEligibilityBlockCode.ManualPullAfterBranchSwitchMissing,
                CloudPushEligibilityBlockCode.LocalBackupDisabledAfterBranchSwitch,
                CloudPushEligibilityBlockCode.BackupStoragePermissionMissing
            },
            result.BlockingReasons.Select(block => block.Code)
        );
        AssertEqual("complete blocker count", 8, result.BlockingReasons.Count);
        AssertEqual("complete unique action count", 7, result.RequiredNextActions.Count);
    }

    private static void RequiredActionsAreDeduplicated()
    {
        var result = Evaluate(
            EligibleState() with
            {
                ManualPullMatchesSelectedVersion = false,
                LocalSaveOriginMatchesSelectedRuntime = false
            }
        );

        AssertEqual("deduplicated blocker count", 2, result.BlockingReasons.Count);
        AssertEqual("deduplicated action count", 1, result.RequiredNextActions.Count);
        AssertEqual(
            "deduplicated action",
            CloudPushRequiredActionCode.CompletePullForSelectedVersion,
            result.RequiredNextActions[0].Code
        );
    }

    private static void BranchFactsAreConditional()
    {
        var result = Evaluate(
            EligibleState() with
            {
                BranchSwitchEvidenceValid = false,
                HasManualPullAfterBranchSwitch = false,
                IsLocalBackupEnabled = false,
                HasBackupStoragePermission = false
            }
        );

        AssertTrue("ordinary launch ignores branch facts", result.IsEligible);
        AssertEqual("ordinary launch branch blockers", 0, result.BlockingReasons.Count);
    }

    private static void MissingPullIsCanonical()
    {
        var result = Evaluate(
            EligibleState() with
            {
                ManualPullCompleted = false,
                ManualPullMatchesSelectedVersion = false
            }
        );

        AssertSequence(
            "canonical missing Pull blocker",
            new[] { CloudPushEligibilityBlockCode.ManualPullNotCompleted },
            result.BlockingReasons.Select(block => block.Code)
        );
    }

    private static void SelectedVersionAppearsInGuidance()
    {
        var result = Evaluate(
            EligibleState() with { ManualPullCompleted = false }
        );

        AssertContains(
            "selected version reason",
            result.BlockingReasons[0].Reason,
            "Public Beta"
        );
        AssertContains(
            "selected version action",
            result.RequiredNextActions[0].Description,
            "Public Beta"
        );
    }

    private static void ResultCollectionsAreImmutable()
    {
        var result = Evaluate(
            EligibleState() with { SelectedModCount = 1 }
        );
        var blocks = (IList<CloudPushEligibilityBlock>)result.BlockingReasons;
        var actions = (IList<CloudPushRequiredAction>)result.RequiredNextActions;

        AssertThrows<NotSupportedException>(
            "blocking reasons mutation",
            () => blocks.Add(default)
        );
        AssertThrows<NotSupportedException>(
            "required actions mutation",
            () => actions.Clear()
        );
    }

    private static CloudPushEligibilityResult Evaluate(
        CloudPushEligibilityState state
    )
        => CloudPushEligibilityPolicy.Evaluate(state);

    private static CloudPushEligibilityState EligibleState()
        => new(
            "Public Beta",
            0,
            true,
            true,
            true,
            true,
            false,
            true,
            true,
            true,
            true
        );

    private static CloudPushEligibilityState SwitchedState()
        => EligibleState() with { HasBranchSwitchMarker = true };

    private static void Run(string name, Action test)
    {
        test();
        _passed++;
        Console.WriteLine($"[PASS] {name}");
    }

    private static void AssertTrue(string name, bool actual)
    {
        if (!actual)
            throw new InvalidOperationException($"{name}: expected true.");
    }

    private static void AssertFalse(string name, bool actual)
    {
        if (actual)
            throw new InvalidOperationException($"{name}: expected false.");
    }

    private static void AssertEqual<T>(string name, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected {expected}, actual {actual}."
            );
        }
    }

    private static void AssertContains(string name, string actual, string expected)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}' in '{actual}'."
            );
        }
    }

    private static void AssertSequence<T>(
        string name,
        IEnumerable<T> expected,
        IEnumerable<T> actual
    )
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected [{string.Join(", ", expected)}], "
                    + $"actual [{string.Join(", ", actual)}]."
            );
        }
    }

    private static void AssertThrows<TException>(
        string name,
        Action action
    )
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}."
        );
    }
}
