using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudUploadEligibilityTest
{
    private static int _passed;

    private static int Main()
    {
        Run("local saves make Push eligible", LocalSavesMakePushEligible);
        Run("missing local saves block Push", MissingLocalSavesAreBlocked);
        Run("an incomplete Pull blocks Push", IncompletePullIsBlocked);
        Run("guidance does not require Pull or an installed runtime", GuidanceHasNoObsoletePrerequisites);
        Run("result collections are immutable", ResultCollectionsAreImmutable);

        AssertEqual("policy tests passed", 5, _passed);
        Console.WriteLine("Launcher cloud Upload eligibility tests passed 5/5.");
        return 0;
    }

    private static void IncompletePullIsBlocked()
    {
        var result = Evaluate(hasLocalSaves: true, hasIncompletePull: true);
        AssertFalse("incomplete Pull eligibility", result.IsEligible);
        AssertEqual("incomplete Pull blocker count", 1, result.BlockingReasons.Count);
        AssertEqual(
            "incomplete Pull blocker",
            CloudPushEligibilityBlockCode.IncompletePullRequiresRecovery,
            result.BlockingReasons[0].Code
        );
        AssertEqual(
            "incomplete Pull action",
            CloudPushRequiredActionCode.RecoverIncompletePull,
            result.RequiredNextActions[0].Code
        );
    }

    private static void LocalSavesMakePushEligible()
    {
        var result = Evaluate(hasLocalSaves: true);
        AssertTrue("eligible result", result.IsEligible);
        AssertEqual("eligible blocker count", 0, result.BlockingReasons.Count);
        AssertEqual("eligible action count", 0, result.RequiredNextActions.Count);
    }

    private static void MissingLocalSavesAreBlocked()
    {
        var result = Evaluate(hasLocalSaves: false);
        AssertFalse("missing saves eligibility", result.IsEligible);
        AssertEqual("missing saves blocker count", 1, result.BlockingReasons.Count);
        AssertEqual(
            "missing saves blocker",
            CloudPushEligibilityBlockCode.ImportantLocalSavesMissing,
            result.BlockingReasons[0].Code
        );
        AssertEqual("missing saves action count", 1, result.RequiredNextActions.Count);
        AssertEqual(
            "missing saves action",
            CloudPushRequiredActionCode.VerifyAndroidLocalSaves,
            result.RequiredNextActions[0].Code
        );
    }

    private static void GuidanceHasNoObsoletePrerequisites()
    {
        var result = Evaluate(hasLocalSaves: false);
        var text = result.BlockingReasons[0].Reason
            + " "
            + result.RequiredNextActions[0].Description;
        AssertDoesNotContain("Pull prerequisite", text, "Pull");
        AssertDoesNotContain("installation prerequisite", text, "installed");
        AssertDoesNotContain("mod prerequisite", text, "Deselect");
        AssertDoesNotContain("backup permission prerequisite", text, "permission");
    }

    private static void ResultCollectionsAreImmutable()
    {
        var result = Evaluate(hasLocalSaves: false);
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
        bool hasLocalSaves,
        bool hasIncompletePull = false
    )
        => CloudPushEligibilityPolicy.Evaluate(
            new(hasLocalSaves, hasIncompletePull)
        );

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
            throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}.");
    }

    private static void AssertDoesNotContain(string name, string actual, string forbidden)
    {
        if (actual.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{name}: did not expect '{forbidden}' in '{actual}'.");
    }

    private static void AssertThrows<TException>(string name, Action action)
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

        throw new InvalidOperationException($"{name}: expected {typeof(TException).Name}.");
    }
}
