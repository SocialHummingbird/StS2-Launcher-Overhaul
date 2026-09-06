using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void AutomaticRepairRoutingSelectsOnlyLocalRepair()
    {
        using var repairable = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries()
        );
        var ready = 0;
        var automaticRepair = 0;
        var redownloadConfirmation = 0;
        var destructiveReset = 0;

        LauncherDownloadCoordinator.RouteInstalledVersionReadiness(
            LocalPckRepairOperation.ClassifyForLauncherRouting(
                repairable.DataDir,
                repairable.Branch
            ),
            ready: () => ready++,
            automaticRepair: () => automaticRepair++,
            redownloadRequired: () =>
            {
                redownloadConfirmation++;
                destructiveReset++;
            }
        );

        Equal(0, ready, "AutomaticRepair must not continue with stale v1 readiness.");
        Equal(1, automaticRepair, "AutomaticRepair must start the local repair exactly once.");
        Equal(0, redownloadConfirmation, "AutomaticRepair must not show redownload confirmation.");
        Equal(0, destructiveReset, "AutomaticRepair must never invoke destructive reset.");

        automaticRepair = 0;
        using var unrecoverable = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries()
                .Where(entry => entry.Path != ManagedFmodPckForms.GameScenePath)
                .ToArray()
        );
        LauncherDownloadCoordinator.RouteInstalledVersionReadiness(
            LocalPckRepairOperation.ClassifyForLauncherRouting(
                unrecoverable.DataDir,
                unrecoverable.Branch
            ),
            ready: () => ready++,
            automaticRepair: () => automaticRepair++,
            redownloadRequired: () => redownloadConfirmation++
        );

        Equal(0, automaticRepair, "RedownloadRequired must not attempt local repair.");
        Equal(1, redownloadConfirmation, "RedownloadRequired must preserve the confirmation route.");
        Equal(0, destructiveReset, "Routing to confirmation must not reset files before confirmation.");
    }

    private static void AutomaticRepairModelUsesExistingGuardAndCompletion()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        SeedDownloadState(fixture);
        var downloadStatePath = Path.Combine(
            SteamGameInstallPaths.DownloadStateDirectoryPath(
                fixture.DataDir,
                fixture.Branch
            ),
            "123.id"
        );
        var downloadStateBytes = File.ReadAllBytes(downloadStatePath);
        using var model = new LauncherModel(fixture.DataDir);
        BranchInstallCompletion? completion = null;
        LauncherBranchOperationFailure? failure = null;
        var steamSessionTransitions = 0;
        var progressMessages = new List<string>();
        model.DownloadCompleted += value => completion = value;
        model.DownloadFailed += value => failure = value;
        model.SessionStateChanged += _ => steamSessionTransitions++;
        model.DownloadProgressChanged += value => value.ApplyTo(
            (_, text) => progressMessages.Add(text),
            _ => { }
        );

        Equal(
            InstalledGameVersionReadiness.AutomaticRepair,
            LocalPckRepairOperation.ClassifyForLauncherRouting(
                fixture.DataDir,
                fixture.Branch
            ),
            "The production-shaped v1 fixture must enter the real automatic-repair route."
        );

        model.StartDownloadAsync(fixture.Branch)
            .GetAwaiter()
            .GetResult();

        True(failure == null, $"Local repair unexpectedly failed: {failure?.Message}");
        True(completion != null, "Local repair must use the existing DownloadCompleted event.");
        Equal(
            0,
            steamSessionTransitions,
            "Normal launcher classification must start local repair without beginning a Steam session."
        );
        Equal(
            AndroidPckPreparationVersions.V2,
            BranchInstallStateStore.Current.Read(
                fixture.DataDir,
                fixture.Branch
            ).PckPreparationVersion,
            "Guarded local repair must publish v2 readiness."
        );
        True(File.Exists(fixture.PckPath), "Automatic repair must not delete the selected PCK.");
        True(
            downloadStateBytes.AsSpan().SequenceEqual(File.ReadAllBytes(downloadStatePath)),
            "Automatic repair must not invoke the redownload reset path."
        );
        True(
            progressMessages.Contains(LocalPckRepairOperation.ProgressMessage),
            "Automatic repair must publish the exact preparation progress text."
        );
    }

    private static void InterruptedRepairRoutesBackToAutomaticRepair()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        using (BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            targetDepots: null,
            phase: LocalPckRepairOperation.StartingPhase
        ))
        {
        }

        Equal(
            InstalledGameVersionReadiness.AutomaticRepair,
            LocalPckRepairOperation.ClassifyForLauncherRouting(
                fixture.DataDir,
                fixture.Branch
            ),
            "An interrupted local repair must route back into the same automatic operation."
        );
    }
}
