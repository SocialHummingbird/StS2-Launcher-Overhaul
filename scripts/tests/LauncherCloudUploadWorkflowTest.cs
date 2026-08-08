using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudUploadWorkflowTest
{
    private static int _passed;

    private static int Main()
    {
        Run("eligible Upload guidance is explicit", EligibleGuidanceIsExplicit);
        Run("missing saves guidance is complete", MissingSavesGuidanceIsComplete);
        Run("guidance has a clear reading order", GuidanceReadingOrderIsClear);

        AssertEqual("workflow tests passed", 3, _passed);
        Console.WriteLine("Launcher cloud Upload workflow tests passed 3/3.");
        return 0;
    }

    private static void EligibleGuidanceIsExplicit()
    {
        var presentation = Present(hasLocalSaves: true);
        AssertTrue("eligible presentation", presentation.IsEligible);
        AssertEqual(
            "eligible review detail",
            "Local saves found",
            presentation.ReviewButtonDetail
        );
        AssertContains("eligible heading", presentation.GuidanceText, "Upload available.");
        AssertContains("local save evidence", presentation.GuidanceText, "local saves are present");
        AssertContains(
            "eligible instruction",
            presentation.GuidanceText,
            "Review the overwrite warning before uploading."
        );
    }

    private static void MissingSavesGuidanceIsComplete()
    {
        var result = Evaluate(hasLocalSaves: false);
        var presentation = CloudPushEligibilityPresentation.Create(result);

        AssertEqual("single blocker count", 1, result.BlockingReasons.Count);
        AssertEqual("single action count", 1, result.RequiredNextActions.Count);
        AssertEqual("single blocker review detail", "1 check to fix", presentation.ReviewButtonDetail);
        AssertContains("single blocker grammar", presentation.GuidanceText, "1 safety check needs attention.");
        AssertContains(
            "reason displayed",
            presentation.GuidanceText,
            result.BlockingReasons[0].Reason
        );
        AssertContains(
            "action displayed",
            presentation.GuidanceText,
            result.RequiredNextActions[0].Description
        );
    }

    private static void GuidanceReadingOrderIsClear()
    {
        var presentation = Present(hasLocalSaves: false);
        var unavailable = presentation.GuidanceText.IndexOf("Upload unavailable:", StringComparison.Ordinal);
        var why = presentation.GuidanceText.IndexOf("Why upload is unavailable:", StringComparison.Ordinal);
        var unlock = presentation.GuidanceText.IndexOf("How to unlock upload:", StringComparison.Ordinal);

        AssertTrue("unavailable heading first", unavailable == 0);
        AssertTrue("reason heading follows status", why > unavailable);
        AssertTrue("unlock heading follows reasons", unlock > why);
    }

    private static CloudPushEligibilityPresentation Present(bool hasLocalSaves)
        => CloudPushEligibilityPresentation.Create(Evaluate(hasLocalSaves));

    private static CloudPushEligibilityResult Evaluate(bool hasLocalSaves)
        => CloudPushEligibilityPolicy.Evaluate(new(hasLocalSaves));

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
            throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}.");
    }

    private static void AssertContains(string name, string actual, string expected)
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{name}: expected '{expected}' in guidance.");
    }
}
