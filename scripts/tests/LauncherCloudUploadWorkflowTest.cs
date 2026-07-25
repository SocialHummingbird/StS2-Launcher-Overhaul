using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudUploadWorkflowTest
{
    private static int _passed;

    private static int Main()
    {
        Run("eligible Upload guidance is explicit", EligibleGuidanceIsExplicit);
        Run("every blocker is displayed", EveryBlockerIsDisplayed);
        Run("every unique unlock action is displayed", EveryActionIsDisplayed);
        Run("guidance has a clear reading order", GuidanceReadingOrderIsClear);
        Run("single blocker grammar is correct", SingleBlockerGrammarIsCorrect);
        Run("duplicate remediation is shown once", DuplicateActionIsShownOnce);

        AssertEqual("workflow tests passed", 6, _passed);
        Console.WriteLine("Launcher cloud Upload workflow tests passed 6/6.");
        return 0;
    }

    private static void EligibleGuidanceIsExplicit()
    {
        var presentation = Present(EligibleState());
        AssertTrue("eligible presentation", presentation.IsEligible);
        AssertEqual(
            "eligible review detail",
            "Safety checks passed",
            presentation.ReviewButtonDetail
        );
        AssertContains(
            "eligible heading",
            presentation.GuidanceText,
            "Upload available."
        );
        AssertContains(
            "eligible instruction",
            presentation.GuidanceText,
            "Review the overwrite warning before uploading."
        );
        AssertDoesNotContain(
            "eligible blocked heading",
            presentation.GuidanceText,
            "Why upload is unavailable"
        );
    }

    private static void EveryBlockerIsDisplayed()
    {
        var result = Evaluate(AllBlockedState());
        var presentation = CloudPushEligibilityPresentation.Create(result);

        AssertEqual("aggregate blocker count", 8, result.BlockingReasons.Count);
        AssertEqual(
            "aggregate review detail",
            "8 checks to fix",
            presentation.ReviewButtonDetail
        );
        foreach (var block in result.BlockingReasons)
        {
            AssertEqual(
                $"{block.Code} reason occurrence",
                1,
                Occurrences(presentation.GuidanceText, block.Reason)
            );
        }
    }

    private static void EveryActionIsDisplayed()
    {
        var result = Evaluate(AllBlockedState());
        var presentation = CloudPushEligibilityPresentation.Create(result);

        AssertEqual("aggregate unique action count", 7, result.RequiredNextActions.Count);
        foreach (var action in result.RequiredNextActions)
        {
            AssertEqual(
                $"{action.Code} action occurrence",
                1,
                Occurrences(presentation.GuidanceText, action.Description)
            );
        }
    }

    private static void GuidanceReadingOrderIsClear()
    {
        var presentation = Present(AllBlockedState());
        var unavailable = presentation.GuidanceText.IndexOf(
            "Upload unavailable:",
            StringComparison.Ordinal
        );
        var why = presentation.GuidanceText.IndexOf(
            "Why upload is unavailable:",
            StringComparison.Ordinal
        );
        var unlock = presentation.GuidanceText.IndexOf(
            "How to unlock upload:",
            StringComparison.Ordinal
        );

        AssertTrue("unavailable heading first", unavailable == 0);
        AssertTrue("reason heading follows status", why > unavailable);
        AssertTrue("unlock heading follows reasons", unlock > why);
    }

    private static void SingleBlockerGrammarIsCorrect()
    {
        var presentation = Present(
            EligibleState() with { SelectedModCount = 1 }
        );

        AssertEqual(
            "single blocker review detail",
            "1 check to fix",
            presentation.ReviewButtonDetail
        );
        AssertContains(
            "single blocker grammar",
            presentation.GuidanceText,
            "1 safety check needs attention."
        );
        AssertDoesNotContain(
            "single blocker plural",
            presentation.GuidanceText,
            "1 safety checks"
        );
    }

    private static void DuplicateActionIsShownOnce()
    {
        var result = Evaluate(
            EligibleState() with
            {
                ManualPullMatchesSelectedVersion = false,
                LocalSaveOriginMatchesSelectedRuntime = false
            }
        );
        var presentation = CloudPushEligibilityPresentation.Create(result);
        var action = result.RequiredNextActions[0];

        AssertEqual("duplicate-remediation blocker count", 2, result.BlockingReasons.Count);
        AssertEqual("duplicate-remediation action count", 1, result.RequiredNextActions.Count);
        AssertEqual(
            "duplicate-remediation guidance occurrence",
            1,
            Occurrences(presentation.GuidanceText, action.Description)
        );
    }

    private static CloudPushEligibilityPresentation Present(
        CloudPushEligibilityState state
    )
        => CloudPushEligibilityPresentation.Create(Evaluate(state));

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

    private static CloudPushEligibilityState AllBlockedState()
        => EligibleState() with
        {
            SelectedModCount = 3,
            ManualPullMatchesSelectedVersion = false,
            HasImportantLocalSaveEvidence = false,
            LocalSaveOriginMatchesSelectedRuntime = false,
            HasBranchSwitchMarker = true,
            BranchSwitchEvidenceValid = false,
            HasManualPullAfterBranchSwitch = false,
            IsLocalBackupEnabled = false,
            HasBackupStoragePermission = false
        };

    private static int Occurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;
        while (offset < text.Length)
        {
            var found = text.IndexOf(value, offset, StringComparison.Ordinal);
            if (found < 0)
                return count;

            count++;
            offset = found + value.Length;
        }

        return count;
    }

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
                $"{name}: expected '{expected}' in guidance."
            );
        }
    }

    private static void AssertDoesNotContain(
        string name,
        string actual,
        string forbidden
    )
    {
        if (actual.Contains(forbidden, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{name}: did not expect '{forbidden}' in guidance."
            );
        }
    }
}
