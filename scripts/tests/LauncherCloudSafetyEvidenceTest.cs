using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudSafetyEvidenceTest
{
    private static int _passed;

    private static int Main()
    {
        Run("successful Pull can satisfy Upload evidence", SuccessfulPullUnlocks);
        Run("Pull start invalidates prior completion", PullStartInvalidatesPriorSuccess);
        Run("partial Pull remains ineligible", PartialPullRemainsIneligible);
        Run("save-origin failure remains ineligible", SaveOriginFailureRemainsIneligible);
        Run("branch identity and ordering remain enforced", BranchIdentityAndOrderingRemainEnforced);
        Run("pre-Push backup evidence fails closed", PrePushBackupEvidenceFailsClosed);

        AssertEqual("safety evidence tests passed", 6, _passed);
        Console.WriteLine("Launcher cloud safety evidence tests passed 6/6.");
        return 0;
    }

    private static void SuccessfulPullUnlocks()
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
            AssertTrue(
                "successful Pull eligibility",
                EvaluateFromEvidence(dataDir, "public").IsEligible
            );
        });

    private static void PullStartInvalidatesPriorSuccess()
        => WithDataDir(dataDir =>
        {
            LauncherSaveOriginEvidence.OriginWriteSucceeds = true;
            AssertTrue(
                "seed successful Pull",
                LauncherCloudSyncEvidence.WriteManualPullMarker(
                    dataDir,
                    "public"
                )
            );
            AssertTrue(
                "begin Pull",
                LauncherCloudSyncEvidence.BeginManualPull(
                    dataDir,
                    "public"
                )
            );

            AssertFalse(
                "pending Pull retained completion",
                LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(
                    dataDir
                )
            );
            AssertEqual("pending Pull outcome", "pending", ReadOutcome(dataDir));
            AssertBlockedByMissingPull(dataDir, "public");
        });

    private static void PartialPullRemainsIneligible()
        => WithDataDir(dataDir =>
        {
            LauncherSaveOriginEvidence.OriginWriteSucceeds = true;
            LauncherCloudSyncEvidence.WriteManualPullMarker(
                dataDir,
                "public"
            );
            LauncherCloudSyncEvidence.BeginManualPull(
                dataDir,
                "public"
            );
            AssertTrue(
                "partial Pull marker write",
                LauncherCloudSyncEvidence.WriteManualPullIncompleteMarker(
                    dataDir,
                    "public",
                    "partial-success",
                    "completed=2; failed=1; unfinished=3"
                )
            );

            AssertFalse(
                "partial Pull retained completion",
                LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(
                    dataDir
                )
            );
            AssertEqual(
                "partial Pull outcome",
                "partial-success",
                ReadOutcome(dataDir)
            );
            AssertBlockedByMissingPull(dataDir, "public");
        });

    private static void SaveOriginFailureRemainsIneligible()
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
            var eligibility = EvaluateFromEvidence(dataDir, "public");
            AssertSequence(
                "origin-failed Pull blockers",
                new[]
                {
                    CloudPushEligibilityBlockCode.ManualPullNotCompleted,
                    CloudPushEligibilityBlockCode.LocalSaveOriginNotVerified
                },
                eligibility.BlockingReasons.Select(block => block.Code)
            );
        });

    private static void BranchIdentityAndOrderingRemainEnforced()
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
                "partial-success",
                "unfinished=1"
            );
            AssertFalse(
                "partial Pull satisfied branch-switch ordering",
                LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(
                    dataDir,
                    "public-beta"
                )
            );
        });

    private static void PrePushBackupEvidenceFailsClosed()
    {
        ManualPushBackupSafetyPolicy.EnsureSatisfied(
            localBackupEnabled: false,
            hasStoragePermission: false,
            importantLocalSaveCount: 2,
            localBackupCount: 0,
            importantCloudSaveCount: 2,
            cloudBackupCount: 0
        );

        AssertThrows<InvalidOperationException>(
            "missing backup storage",
            () => ManualPushBackupSafetyPolicy.EnsureSatisfied(
                localBackupEnabled: true,
                hasStoragePermission: false,
                importantLocalSaveCount: 1,
                localBackupCount: 1,
                importantCloudSaveCount: 1,
                cloudBackupCount: 1
            ),
            "storage permission"
        );
        AssertThrows<InvalidOperationException>(
            "incomplete local pre-Push backup",
            () => ManualPushBackupSafetyPolicy.EnsureSatisfied(
                localBackupEnabled: true,
                hasStoragePermission: true,
                importantLocalSaveCount: 2,
                localBackupCount: 1,
                importantCloudSaveCount: 1,
                cloudBackupCount: 1
            ),
            "local pre-Push backup"
        );
        AssertThrows<InvalidOperationException>(
            "incomplete cloud pre-Push backup",
            () => ManualPushBackupSafetyPolicy.EnsureSatisfied(
                localBackupEnabled: true,
                hasStoragePermission: true,
                importantLocalSaveCount: 2,
                localBackupCount: 2,
                importantCloudSaveCount: 2,
                cloudBackupCount: 1
            ),
            "cloud pre-Push backup"
        );

        ManualPushBackupSafetyPolicy.EnsureSatisfied(
            localBackupEnabled: true,
            hasStoragePermission: true,
            importantLocalSaveCount: 2,
            localBackupCount: 2,
            importantCloudSaveCount: 2,
            cloudBackupCount: 2
        );
    }

    private static CloudPushEligibilityResult EvaluateFromEvidence(
        string dataDir,
        string selectedBranch
    )
        => CloudPushEligibilityPolicy.Evaluate(
            new CloudPushEligibilityState(
                selectedBranch,
                0,
                LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(
                    dataDir
                ),
                LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(
                    dataDir,
                    selectedBranch
                ),
                true,
                LauncherSaveOriginEvidence
                    .CurrentLocalSavesMatchSelectedRuntime(
                        dataDir,
                        selectedBranch
                    ),
                false,
                true,
                true,
                true,
                true
            )
        );

    private static void AssertBlockedByMissingPull(
        string dataDir,
        string selectedBranch
    )
    {
        var result = EvaluateFromEvidence(dataDir, selectedBranch);
        AssertFalse("incomplete Pull eligibility", result.IsEligible);
        AssertSequence(
            "incomplete Pull blocker",
            new[] { CloudPushEligibilityBlockCode.ManualPullNotCompleted },
            result.BlockingReasons.Select(block => block.Code)
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
        Action action,
        string expectedMessage
    )
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            if (
                !ex.Message.Contains(
                    expectedMessage,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                throw new InvalidOperationException(
                    $"{name}: expected '{expectedMessage}' in '{ex.Message}'."
                );
            }

            return;
        }

        throw new InvalidOperationException(
            $"{name}: expected {typeof(TException).Name}."
        );
    }
}
