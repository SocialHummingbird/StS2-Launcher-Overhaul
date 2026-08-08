using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudSafetyEvidenceTest
{
    private static int _passed;

    private static int Main()
    {
        Run("successful Pull records diagnostic evidence", SuccessfulPullRecordsEvidence);
        Run("failed Pull records a diagnostic outcome", FailedPullRecordsDiagnosticOutcome);
        Run("save-origin failure records diagnostic failure", SaveOriginFailureRecordsEvidenceFailure);
        Run("diagnostic marker tracks branch identity", BranchEvidenceTracksIdentityAndOrdering);
        Run("only live transfer state controls Upload eligibility", LocalSavePresenceControlsEligibility);

        AssertEqual("diagnostic evidence tests passed", 5, _passed);
        Console.WriteLine("Launcher cloud diagnostic evidence tests passed 5/5.");
        return 0;
    }

    private static void SuccessfulPullRecordsEvidence()
        => WithDataDir(dataDir =>
        {
            LauncherSaveOriginEvidence.OriginWriteSucceeds = true;
            AssertTrue(
                "successful Pull marker write",
                LauncherCloudSyncEvidence.WriteManualPullMarker(
                    dataDir,
                    "public"
                )
            );
            AssertTrue(
                "successful Pull completion",
                LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(
                    dataDir
                )
            );
            AssertEqual(
                "successful Pull outcome",
                "success",
                ReadOutcome(dataDir)
            );
            var marker = File.ReadAllText(
                LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir)
            );
            AssertFalse(
                "Pull history claims it is a Push prerequisite",
                marker.Contains("before Push", StringComparison.OrdinalIgnoreCase)
            );
            AssertFalse(
                "Pull history claims branch-switch Push gating",
                marker.Contains("branch-switch Push", StringComparison.OrdinalIgnoreCase)
            );
        });

    private static void FailedPullRecordsDiagnosticOutcome()
        => WithDataDir(dataDir =>
        {
            AssertTrue(
                "failed Pull marker write",
                LauncherCloudSyncEvidence.WriteManualPullIncompleteMarker(
                    dataDir,
                    "public",
                    "failure",
                    "Steam connection failed"
                )
            );

            AssertFalse(
                "failed Pull recorded completion",
                LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(
                    dataDir
                )
            );
            AssertEqual(
                "failed Pull outcome",
                "failure",
                ReadOutcome(dataDir)
            );
        });

    private static void SaveOriginFailureRecordsEvidenceFailure()
        => WithDataDir(dataDir =>
        {
            LauncherSaveOriginEvidence.OriginWriteSucceeds = false;
            AssertFalse(
                "origin-failed Pull reported success",
                LauncherCloudSyncEvidence.WriteManualPullMarker(
                    dataDir,
                    "public"
                )
            );

            AssertFalse(
                "origin-failed Pull recorded completion",
                LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(
                    dataDir
                )
            );
            AssertEqual(
                "origin-failed Pull outcome",
                "evidence-failure",
                ReadOutcome(dataDir)
            );
        });

    private static void BranchEvidenceTracksIdentityAndOrdering()
        => WithDataDir(dataDir =>
        {
            LauncherSaveOriginEvidence.OriginWriteSucceeds = true;
            File.WriteAllText(
                LauncherBranchSwitchSafety.MarkerPath(dataDir),
                $"UTC: {DateTime.UtcNow.AddMinutes(-1):O}\n"
            );
            LauncherCloudSyncEvidence.WriteManualPullMarker(
                dataDir,
                "public-beta"
            );

            AssertTrue(
                "matching selected branch",
                LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(
                    dataDir,
                    "public-beta"
                )
            );
            AssertFalse(
                "mismatching selected branch",
                LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(
                    dataDir,
                    "public"
                )
            );
            AssertTrue(
                "completed Pull after branch switch",
                LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(
                    dataDir,
                    "public-beta"
                )
            );

            LauncherCloudSyncEvidence.WriteManualPullIncompleteMarker(
                dataDir,
                "public-beta",
                "failure",
                "Steam connection failed"
            );
            AssertFalse(
                "failed Pull satisfied branch-switch ordering",
                LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(
                    dataDir,
                    "public-beta"
                )
            );
        });

    private static void LocalSavePresenceControlsEligibility()
    {
        var eligible = CloudPushEligibilityPolicy.Evaluate(
            new CloudPushEligibilityState(true)
        );
        AssertTrue("local saves allow Upload", eligible.IsEligible);

        var blocked = CloudPushEligibilityPolicy.Evaluate(
            new CloudPushEligibilityState(false)
        );
        AssertFalse("missing local saves allow Upload", blocked.IsEligible);
        AssertEqual("missing local save blocker count", 1, blocked.BlockingReasons.Count);
        AssertEqual(
            "missing local save blocker",
            CloudPushEligibilityBlockCode.ImportantLocalSavesMissing,
            blocked.BlockingReasons[0].Code
        );
        AssertEqual(
            "missing local save action",
            CloudPushRequiredActionCode.VerifyAndroidLocalSaves,
            blocked.RequiredNextActions[0].Code
        );
    }

    private static string ReadOutcome(string dataDir)
        => LauncherMarkerFile.ReadValue(
            LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir),
            LauncherCloudSyncEvidence.ManualPullOutcomePrefix
        );

    private static void WithDataDir(Action<string> test)
    {
        var dataDir = Path.Combine(
            Path.GetTempPath(),
            "sts2-cloud-safety-" + Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(dataDir);
        try
        {
            LauncherSaveOriginEvidence.OriginWriteSucceeds = true;
            test(dataDir);
        }
        finally
        {
            Directory.Delete(dataDir, recursive: true);
        }
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

}
