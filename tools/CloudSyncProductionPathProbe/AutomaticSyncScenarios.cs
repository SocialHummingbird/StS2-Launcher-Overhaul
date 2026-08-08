#nullable enable

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal static class AutomaticSyncScenarios
{
    private const ulong SteamId64 = InMemoryCloudSaveStore.DefaultSteamId64;
    private const string ProgressPath = "profile1/saves/progress.save";
    private const string PreferencesPath = "profile1/saves/prefs.save";
    private const string VanillaIncompletePullPath =
        ".sts2-launcher/pull-incomplete/vanilla.json";
    private const string ModdedProgressPath =
        "modded/profile1/saves/progress.save";
    private const string SettingsPath = "settings.save";
    private const string PublicBeta = "public-beta";
    private const string ModFingerprintA = "sha256:mods-a";
    private const string ModFingerprintB = "sha256:mods-b";
    private const string A = "{\"progress\":\"a\"}";
    private const string B = "{\"progress\":\"b\"}";
    private const string C = "{\"progress\":\"c\"}";
    private const string PrefsA = "{\"prefs\":\"a\"}";
    private const string PrefsB = "{\"prefs\":\"b\"}";
    private static int _passed;

    internal static async Task<int> RunAllAsync()
    {
        await RunAsync(
            "automatic first sync requires and honors either explicit source",
            ExplicitFirstSourceChoiceBothWaysAsync
        );
        await RunAsync(
            "automatic three-way comparison never guesses",
            ThreeWayDecisionMatrixAsync
        );
        await RunAsync(
            "before-game snapshot, baselines, and pending record are durable",
            DurableBeforeGameStateAsync
        );
        await RunAsync(
            "launcher recovery performs normal post-game sync",
            QuitStyleRecoveryAsync
        );
        await RunAsync(
            "offline work stays pending and retry rereads Steam",
            OfflineRetentionAndFreshRemoteReadAsync
        );
        await RunAsync(
            "interrupted upload handles unchanged, committed, partial, and independent remote state",
            InterruptedUploadStatesAsync
        );
        await RunAsync(
            "interrupted Pull resumes only a baseline-to-target blend",
            InterruptedPullStatesAsync
        );
        await RunAsync(
            "restart is safe before and after every observed persistence transition",
            PersistenceEdgeRestartMatrixAsync
        );
        await RunAsync(
            "automatic snapshots are context-separated and unresolved work gates launch",
            ContextSeparationAndLaunchGateAsync
        );
        await RunAsync(
            "only a prepared game session permits launch",
            ResultSemanticsAsync
        );
        await RunAsync(
            "version 2 retained snapshots preserve exact local bytes",
            RawByteSnapshotAsync
        );
        await RunAsync(
            "snapshot payload and entry corruption are rejected",
            SnapshotCorruptionIsRejectedAsync
        );
        await RunAsync(
            "retained snapshots are bounded without pruning protected evidence",
            RetainedSnapshotPruningAsync
        );
        await RunAsync(
            "version 1 fixed before-game snapshots remain recoverable",
            LegacyBeforeGameFallbackAsync
        );
        await RunAsync(
            "automatic sync emits exact structured terminal evidence",
            StructuredTerminalEvidenceAsync
        );

        Console.WriteLine(
            $"Automatic sync production-path probe passed {_passed}/15 scenarios."
        );
        return _passed;
    }

    private static async Task ExplicitFirstSourceChoiceBothWaysAsync()
    {
        var local = Store("choice-local");
        var cloud = Store("choice-cloud");
        var context = VanillaContext();
        local.Seed(ProgressPath, B);
        cloud.Seed(ProgressPath, A);
        SeedMarker(cloud, context);

        var required = await ReconcileAsync(local, cloud).ConfigureAwait(false);
        ExpectOutcome(required, AutomaticSyncOutcome.SourceChoiceRequired);
        ExpectContent(local, ProgressPath, B);
        ExpectContent(cloud, ProgressPath, A);
        ExpectMissing(local, CloudSyncCoordinator.AutomaticSyncBaselinePath(context));

        var choseLocal = await ReconcileAsync(
            local,
            cloud,
            AutomaticSyncSourceChoice.Local
        ).ConfigureAwait(false);
        ExpectOutcome(choseLocal, AutomaticSyncOutcome.Synchronized);
        ExpectContent(cloud, ProgressPath, B);
        ExpectJson(local, CloudSyncCoordinator.AutomaticSyncBaselinePath(context));

        var prepared = await BeginAsync(local, cloud).ConfigureAwait(false);
        ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
        Expect(prepared.CanStartGame, "Explicit local choice did not unlock play.");
        await ExpectRecoveredAsync(local, cloud).ConfigureAwait(false);

        var steamLocal = Store("choice-steam-local");
        var steamCloud = Store("choice-steam-cloud");
        steamLocal.Seed(ProgressPath, A);
        steamCloud.Seed(ProgressPath, B);

        required = await ReconcileAsync(steamLocal, steamCloud)
            .ConfigureAwait(false);
        ExpectOutcome(required, AutomaticSyncOutcome.SourceChoiceRequired);
        var choseSteam = await ReconcileAsync(
            steamLocal,
            steamCloud,
            AutomaticSyncSourceChoice.Steam
        ).ConfigureAwait(false);
        ExpectOutcome(choseSteam, AutomaticSyncOutcome.Synchronized);
        ExpectContent(steamLocal, ProgressPath, B);
        ExpectContent(steamCloud, context.MarkerPath, context.SerializeMarker());
        Expect(
            HasTransferBackup(steamLocal, ProgressPath),
            "Explicit Steam choice overwrote local data without a backup."
        );
        ExpectJson(
            steamLocal,
            CloudSyncCoordinator.AutomaticSyncBaselinePath(context)
        );

        var emptyLocal = Store("choice-empty-local-local");
        var populatedCloud = Store("choice-empty-local-cloud");
        populatedCloud.Seed(ProgressPath, A);
        populatedCloud.Seed(PreferencesPath, PrefsA);
        required = await ReconcileAsync(emptyLocal, populatedCloud)
            .ConfigureAwait(false);
        ExpectOutcome(required, AutomaticSyncOutcome.SourceChoiceRequired);
        var choseEmptyLocal = await ReconcileAsync(
            emptyLocal,
            populatedCloud,
            AutomaticSyncSourceChoice.Local
        ).ConfigureAwait(false);
        ExpectOutcome(choseEmptyLocal, AutomaticSyncOutcome.Synchronized);
        ExpectMissing(populatedCloud, ProgressPath);
        ExpectMissing(populatedCloud, PreferencesPath);
        ExpectContent(
            populatedCloud,
            context.MarkerPath,
            context.SerializeMarker()
        );
        Expect(
            HasTransferBackup(emptyLocal, ProgressPath)
                && HasTransferBackup(emptyLocal, PreferencesPath),
            "Explicit empty Local choice deleted Steam saves without backups."
        );
        ExpectJson(
            emptyLocal,
            CloudSyncCoordinator.AutomaticSyncBaselinePath(context)
        );
        ExpectBaselineMissing(
            emptyLocal,
            context,
            ProgressPath,
            PreferencesPath
        );
        ExpectMissing(
            emptyLocal,
            CloudSyncCoordinator.AutomaticSyncPendingPath
        );

        var populatedLocal = Store("choice-empty-steam-local");
        var emptyCloud = Store("choice-empty-steam-cloud");
        populatedLocal.Seed(ProgressPath, A);
        populatedLocal.Seed(PreferencesPath, PrefsA);
        required = await ReconcileAsync(populatedLocal, emptyCloud)
            .ConfigureAwait(false);
        ExpectOutcome(required, AutomaticSyncOutcome.SourceChoiceRequired);
        var choseEmptySteam = await ReconcileAsync(
            populatedLocal,
            emptyCloud,
            AutomaticSyncSourceChoice.Steam
        ).ConfigureAwait(false);
        ExpectOutcome(choseEmptySteam, AutomaticSyncOutcome.Synchronized);
        ExpectMissing(populatedLocal, ProgressPath);
        ExpectMissing(populatedLocal, PreferencesPath);
        ExpectContent(
            emptyCloud,
            context.MarkerPath,
            context.SerializeMarker()
        );
        Expect(
            HasTransferBackup(populatedLocal, ProgressPath)
                && HasTransferBackup(populatedLocal, PreferencesPath),
            "Explicit empty Steam choice deleted Android saves without backups."
        );
        ExpectJson(
            populatedLocal,
            CloudSyncCoordinator.AutomaticSyncBaselinePath(context)
        );
        ExpectBaselineMissing(
            populatedLocal,
            context,
            ProgressPath,
            PreferencesPath
        );
        ExpectMissing(
            populatedLocal,
            CloudSyncCoordinator.AutomaticSyncPendingPath
        );
        ExpectMissing(populatedLocal, VanillaIncompletePullPath);
        var emptyPrepared = await BeginAsync(populatedLocal, emptyCloud)
            .ConfigureAwait(false);
        ExpectOutcome(
            emptyPrepared,
            AutomaticSyncOutcome.GameSessionPrepared
        );
        await ExpectRecoveredAsync(populatedLocal, emptyCloud)
            .ConfigureAwait(false);

        foreach (var edge in new[]
                 {
                     StoreMutationEdge.Before,
                     StoreMutationEdge.After,
                 })
        {
            var edgeName = edge == StoreMutationEdge.Before ? "before" : "after";
            var restartLocal = Store($"choice-steam-{edgeName}-local");
            var restartCloud = Store($"choice-steam-{edgeName}-cloud");
            restartLocal.Seed(ProgressPath, A);
            restartCloud.Seed(ProgressPath, B);
            var captured = await CaptureAtMutationAsync(
                restartLocal,
                restartCloud,
                mutation => mutation.Store == $"choice-steam-{edgeName}-cloud"
                    && mutation.Kind == StoreMutationKind.Write
                    && mutation.Edge == edge
                    && mutation.Path.Equals(
                        context.MarkerPath,
                        StringComparison.OrdinalIgnoreCase
                    ),
                () => ReconcileAsync(
                    restartLocal,
                    restartCloud,
                    AutomaticSyncSourceChoice.Steam
                ),
                $"{edgeName} first-time Steam marker adoption"
            ).ConfigureAwait(false);
            ExpectJson(
                captured.Local,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            );
            if (edge == StoreMutationEdge.Before)
                ExpectMissing(captured.Cloud, context.MarkerPath);
            else
                ExpectContent(
                    captured.Cloud,
                    context.MarkerPath,
                    context.SerializeMarker()
                );
            await ExpectRecoveredAsync(captured.Local, captured.Cloud)
                .ConfigureAwait(false);
            ExpectContent(captured.Local, ProgressPath, B);
            ExpectContent(
                captured.Cloud,
                context.MarkerPath,
                context.SerializeMarker()
            );
        }

        var unreadableLocal = Store("choice-steam-unreadable-local");
        var unreadableCloud = Store("choice-steam-unreadable-cloud");
        unreadableLocal.Seed(ProgressPath, A);
        unreadableCloud.Seed(ProgressPath, B);
        unreadableCloud.Seed(context.MarkerPath, "not-json");
        var blocked = await ReconcileAsync(
            unreadableLocal,
            unreadableCloud,
            AutomaticSyncSourceChoice.Steam
        ).ConfigureAwait(false);
        ExpectOutcome(blocked, AutomaticSyncOutcome.Conflict);
        ExpectContent(unreadableLocal, ProgressPath, A);
        ExpectContent(unreadableCloud, context.MarkerPath, "not-json");
        ExpectMissing(
            unreadableLocal,
            CloudSyncCoordinator.AutomaticSyncPendingPath
        );

        var mismatchLocal = Store("choice-steam-mismatch-local");
        var mismatchCloud = Store("choice-steam-mismatch-cloud");
        mismatchLocal.Seed(ProgressPath, A);
        mismatchCloud.Seed(ProgressPath, B);
        SeedMarker(mismatchCloud, VanillaContext(PublicBeta));
        blocked = await ReconcileAsync(
            mismatchLocal,
            mismatchCloud,
            AutomaticSyncSourceChoice.Steam
        ).ConfigureAwait(false);
        ExpectOutcome(blocked, AutomaticSyncOutcome.Conflict);
        ExpectContent(mismatchLocal, ProgressPath, A);
        ExpectContent(
            mismatchCloud,
            context.MarkerPath,
            VanillaContext(PublicBeta).SerializeMarker()
        );
        ExpectMissing(
            mismatchLocal,
            CloudSyncCoordinator.AutomaticSyncPendingPath
        );
    }

    private static async Task StructuredTerminalEvidenceAsync()
    {
        var messages = new List<string>();
        void Capture(string message)
        {
            if (message.StartsWith(
                    SaveEvidenceEvents.Marker,
                    StringComparison.Ordinal
                ))
            {
                messages.Add(message);
            }
        }

        PatchHelper.LogEmitted += Capture;
        try
        {
            var local = Store("structured-evidence-local");
            var cloud = Store("structured-evidence-cloud");
            local.Seed(ProgressPath, A);
            cloud.Seed(ProgressPath, A);
            SeedMarker(cloud, VanillaContext());

            var choice = await ReconcileAsync(local, cloud)
                .ConfigureAwait(false);
            ExpectOutcome(choice, AutomaticSyncOutcome.SourceChoiceRequired);
            var synchronized = await ReconcileAsync(
                local,
                cloud,
                AutomaticSyncSourceChoice.Local
            ).ConfigureAwait(false);
            ExpectOutcome(synchronized, AutomaticSyncOutcome.Synchronized);

            var mismatchLocal = Store("structured-mismatch-local");
            var mismatchCloud = Store("structured-mismatch-cloud");
            mismatchLocal.Seed(ModdedProgressPath, A);
            mismatchCloud.Seed(ModdedProgressPath, A);
            SeedMarker(mismatchCloud, ModdedContext(ModFingerprintB));
            var mismatch = await ReconcileAsync(
                mismatchLocal,
                mismatchCloud,
                saveNamespace: SaveNamespace.Modded,
                runtime: PublicBeta,
                modFingerprint: ModFingerprintA
            ).ConfigureAwait(false);
            ExpectOutcome(mismatch, AutomaticSyncOutcome.Conflict);

            var commitLocal = Store("structured-commit-local");
            var commitCloud = Store("structured-commit-cloud");
            commitLocal.Seed(ProgressPath, A);
            commitCloud.Seed(ProgressPath, A);
            SeedMarker(commitCloud, VanillaContext());
            _ = await ReconcileAsync(
                commitLocal,
                commitCloud,
                AutomaticSyncSourceChoice.Local
            ).ConfigureAwait(false);
            await ExpectPreparedAsync(commitLocal, commitCloud)
                .ConfigureAwait(false);
            commitLocal.Seed(ProgressPath, B);
            commitCloud.WriteFailure = path => path.Equals(
                ProgressPath,
                StringComparison.OrdinalIgnoreCase
            )
                ? new CloudFileCommitRejectedException(
                    "planned file_committed=false"
                )
                : null;
            try
            {
                _ = await RecoverAsync(commitLocal, commitCloud)
                    .ConfigureAwait(false);
                throw new InvalidOperationException(
                    "Structured commit fixture unexpectedly succeeded."
                );
            }
            catch (CloudFileCommitRejectedException)
            {
            }

            var readBackLocal = Store("structured-readback-local");
            var readBackCloud = Store("structured-readback-cloud");
            readBackLocal.Seed(ProgressPath, A);
            readBackCloud.Seed(ProgressPath, A);
            SeedMarker(readBackCloud, VanillaContext());
            _ = await ReconcileAsync(
                readBackLocal,
                readBackCloud,
                AutomaticSyncSourceChoice.Local
            ).ConfigureAwait(false);
            await ExpectPreparedAsync(readBackLocal, readBackCloud)
                .ConfigureAwait(false);
            readBackLocal.Seed(ProgressPath, B);
            var writesBefore = readBackCloud.WriteCountFor(ProgressPath);
            readBackCloud.VerificationRawReadTransform = (path, content) =>
                path.Equals(ProgressPath, StringComparison.OrdinalIgnoreCase)
                    && readBackCloud.WriteCountFor(path) > writesBefore
                    ? content.Concat(new byte[] { 0xA5 }).ToArray()
                    : content;
            try
            {
                _ = await RecoverAsync(readBackLocal, readBackCloud)
                    .ConfigureAwait(false);
                throw new InvalidOperationException(
                    "Structured read-back fixture unexpectedly succeeded."
                );
            }
            catch (SaveTransferReadBackMismatchException)
            {
            }
        }
        finally
        {
            PatchHelper.LogEmitted -= Capture;
        }

        ExpectAutomaticTerminal(
            messages,
            "reconcile",
            "source-choice-required",
            "source-choice-required",
            SaveEvidenceEvents.ContextSha256(VanillaContext()),
            remoteVerified: false
        );
        ExpectAutomaticTerminal(
            messages,
            "reconcile",
            "synchronized",
            "verified",
            SaveEvidenceEvents.ContextSha256(VanillaContext()),
            remoteVerified: true
        );
        ExpectAutomaticTerminal(
            messages,
            "reconcile",
            "conflict",
            "mod-set-mismatch",
            SaveEvidenceEvents.ContextSha256(ModdedContext(ModFingerprintA)),
            remoteVerified: false
        );
        ExpectAutomaticTerminal(
            messages,
            "recover",
            "failed",
            "commit-rejected",
            SaveEvidenceEvents.ContextSha256(VanillaContext()),
            remoteVerified: false
        );
        ExpectAutomaticTerminal(
            messages,
            "recover",
            "failed",
            "remote-readback-mismatch",
            SaveEvidenceEvents.ContextSha256(VanillaContext()),
            remoteVerified: false
        );
    }

    private static void ExpectAutomaticTerminal(
        IEnumerable<string> messages,
        string operation,
        string outcome,
        string detail,
        string contextSha256,
        bool remoteVerified
    )
    {
        var matches = 0;
        foreach (var message in messages)
        {
            using var document = JsonDocument.Parse(
                message[SaveEvidenceEvents.Marker.Length..]
            );
            var root = document.RootElement;
            Expect(
                root.ValueKind == JsonValueKind.Object,
                "Automatic terminal evidence is not a JSON object."
            );
            Expect(
                root.EnumerateObject().Count() == 7,
                "Automatic terminal evidence has unexpected fields."
            );
            Expect(root.GetProperty("Event").GetString()
                == "automatic-sync-terminal",
                "Automatic terminal evidence has the wrong event name."
            );
            Expect(root.GetProperty("Version").ValueKind
                == JsonValueKind.Number,
                "Automatic terminal evidence version is not numeric."
            );
            Expect(
                root.GetProperty("Version").GetInt32() == 1,
                "Automatic terminal evidence has the wrong version."
            );
            if (root.GetProperty("Operation").GetString() == operation
                && root.GetProperty("Outcome").GetString() == outcome
                && root.GetProperty("Detail").GetString() == detail
                && root.GetProperty("ContextSha256").GetString()
                    == contextSha256
                && root.GetProperty("RemoteVerified").GetBoolean()
                    == remoteVerified)
            {
                matches++;
            }
        }
        Expect(
            matches > 0,
            $"Missing exact structured terminal {operation}/{outcome}/{detail}."
        );
    }

    private static async Task ThreeWayDecisionMatrixAsync()
    {
        var localOnly = await EstablishedVanillaAsync("local-only")
            .ConfigureAwait(false);
        localOnly.Local.Seed(ProgressPath, B);
        var result = await ReconcileAsync(localOnly.Local, localOnly.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Synchronized);
        ExpectContent(localOnly.Cloud, ProgressPath, B);

        var remoteOnly = await EstablishedVanillaAsync("remote-only")
            .ConfigureAwait(false);
        remoteOnly.Cloud.Seed(ProgressPath, B);
        result = await ReconcileAsync(remoteOnly.Local, remoteOnly.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Synchronized);
        ExpectContent(remoteOnly.Local, ProgressPath, B);
        Expect(
            HasTransferBackup(remoteOnly.Local, ProgressPath),
            "Remote-only change overwrote local data without a backup."
        );

        var bothEqual = await EstablishedVanillaAsync("both-equal")
            .ConfigureAwait(false);
        bothEqual.Local.Seed(ProgressPath, B);
        bothEqual.Cloud.Seed(ProgressPath, B);
        var remoteWrites = bothEqual.Cloud.WriteCountFor(ProgressPath);
        result = await ReconcileAsync(bothEqual.Local, bothEqual.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Synchronized);
        Expect(
            bothEqual.Cloud.WriteCountFor(ProgressPath) == remoteWrites,
            "Equal local and remote contents caused a needless transfer."
        );

        var conflict = await EstablishedVanillaAsync("conflict")
            .ConfigureAwait(false);
        conflict.Local.Seed(ProgressPath, B);
        conflict.Cloud.Seed(ProgressPath, C);
        var localWrites = conflict.Local.WriteCount;
        remoteWrites = conflict.Cloud.WriteCount;
        result = await ReconcileAsync(conflict.Local, conflict.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Conflict);
        Expect(result.HasConflict && !result.CanStartGame, "Conflict allowed play.");
        ExpectContent(conflict.Local, ProgressPath, B);
        ExpectContent(conflict.Cloud, ProgressPath, C);
        Expect(
            conflict.Local.WriteCount == localWrites
                && conflict.Cloud.WriteCount == remoteWrites,
            "Conflict resolution mutated a save instead of stopping."
        );

        var timestampOnly = await EstablishedVanillaAsync("timestamp-only")
            .ConfigureAwait(false);
        timestampOnly.Local.Seed(
            ProgressPath,
            A,
            DateTimeOffset.UtcNow.AddYears(2)
        );
        remoteWrites = timestampOnly.Cloud.WriteCountFor(ProgressPath);
        result = await ReconcileAsync(timestampOnly.Local, timestampOnly.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Synchronized);
        Expect(
            timestampOnly.Cloud.WriteCountFor(ProgressPath) == remoteWrites,
            "A timestamp-only change selected a winner."
        );

        var deletion = await EstablishedVanillaAsync(
            "deletion",
            includePreferences: true
        )
            .ConfigureAwait(false);
        ((ISaveStore)deletion.Local).DeleteFile(ProgressPath);
        result = await ReconcileAsync(deletion.Local, deletion.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Synchronized);
        ExpectMissing(deletion.Cloud, ProgressPath);
    }

    private static async Task DurableBeforeGameStateAsync()
    {
        var fixture = await EstablishedVanillaAsync("durable")
            .ConfigureAwait(false);
        fixture.Local.Seed(SettingsPath, "{\"device\":true}");
        var result = await BeginAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.GameSessionPrepared);

        var baselinePath = CloudSyncCoordinator.AutomaticSyncBaselinePath(
            fixture.Context
        );
        var pendingPath = CloudSyncCoordinator.AutomaticSyncPendingPath;
        var baseline = ExpectJson(fixture.Local, baselinePath);
        var pending = ExpectJson(fixture.Local, pendingPath);
        var beforePath = PendingSnapshotPath(pending);
        var before = ExpectJson(fixture.Local, beforePath);
        ExpectContains(baseline, ProgressPath);
        ExpectContains(before, ProgressPath);
        ExpectSnapshotContent(before, ProgressPath, A);
        ExpectContains(pending, "game-running");
        ExpectContains(pending, "LocalBaseline");
        ExpectContains(pending, "RemoteBaseline");
        Expect(
            !baseline.Contains(SettingsPath, StringComparison.OrdinalIgnoreCase)
                && !before.Contains(SettingsPath, StringComparison.OrdinalIgnoreCase)
                && !pending.Contains(SettingsPath, StringComparison.OrdinalIgnoreCase),
            "Device settings leaked into automatic sync state."
        );
        Expect(
            fixture.Local.Paths.Count(path => path.Equals(
                pendingPath,
                StringComparison.OrdinalIgnoreCase
            )) == 1,
            "Automatic sync created more than one pending record."
        );

        fixture.Local.Seed(ProgressPath, B);
        ExpectContent(fixture.Local, beforePath, before);
        ExpectJson(fixture.Local, pendingPath);
    }

    private static async Task QuitStyleRecoveryAsync()
    {
        var fixture = await EstablishedVanillaAsync("quit-recovery")
            .ConfigureAwait(false);
        var prepared = await BeginAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
        fixture.Local.Seed(ProgressPath, B);

        var recovered = await RecoverAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(recovered, AutomaticSyncOutcome.Synchronized);
        ExpectContent(fixture.Cloud, ProgressPath, B);
        ExpectMissing(fixture.Local, CloudSyncCoordinator.AutomaticSyncPendingPath);
        var baseline = ExpectJson(
            fixture.Local,
            CloudSyncCoordinator.AutomaticSyncBaselinePath(fixture.Context)
        );
        ExpectContains(baseline, AutomaticSyncHash.Compute(B));
    }

    private static async Task OfflineRetentionAndFreshRemoteReadAsync()
    {
        var fixture = await EstablishedVanillaAsync("offline")
            .ConfigureAwait(false);
        await ExpectPreparedAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        fixture.Local.Seed(ProgressPath, B);
        fixture.Cloud.AuthenticationFailure = new IOException(
            "Injected offline Steam connection."
        );

        AutomaticSyncResult? offline = null;
        try
        {
            offline = await RecoverAsync(fixture.Local, fixture.Cloud)
                .ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Propagation is acceptable as long as the durable pending record remains.
        }
        if (offline.HasValue)
        {
            ExpectOutcome(
                offline.Value,
                AutomaticSyncOutcome.PendingRecoveryRequired
            );
        }
        ExpectJson(fixture.Local, CloudSyncCoordinator.AutomaticSyncPendingPath);
        ExpectContent(fixture.Local, ProgressPath, B);

        fixture.Cloud.AuthenticationFailure = null;
        fixture.Cloud.Seed(ProgressPath, C);
        var readsBeforeRetry = fixture.Cloud.ReadCount;
        var retry = await RecoverAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(retry, AutomaticSyncOutcome.Conflict);
        Expect(
            fixture.Cloud.ReadCount > readsBeforeRetry,
            "Recovery reused stale remote hashes instead of rereading Steam."
        );
        ExpectJson(fixture.Local, CloudSyncCoordinator.AutomaticSyncPendingPath);
        ExpectContent(fixture.Local, ProgressPath, B);
        ExpectContent(fixture.Cloud, ProgressPath, C);

        var secondLaunch = await BeginAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(
            secondLaunch,
            AutomaticSyncOutcome.PendingRecoveryRequired
        );
        Expect(!secondLaunch.CanStartGame, "Unresolved offline work allowed play.");
    }

    private static async Task InterruptedUploadStatesAsync()
    {
        var unchanged = await EstablishedVanillaAsync(
            "upload-unchanged",
            includePreferences: true
        ).ConfigureAwait(false);
        await ExpectPreparedAsync(unchanged.Local, unchanged.Cloud)
            .ConfigureAwait(false);
        unchanged.Local.Seed(ProgressPath, B);
        unchanged.Local.Seed(PreferencesPath, PrefsB);
        await ExpectRecoveredAsync(unchanged.Local, unchanged.Cloud)
            .ConfigureAwait(false);
        ExpectContent(unchanged.Cloud, ProgressPath, B);
        ExpectContent(unchanged.Cloud, PreferencesPath, PrefsB);

        var partialFixture = await EstablishedVanillaAsync(
            "upload-partial",
            includePreferences: true
        ).ConfigureAwait(false);
        await ExpectPreparedAsync(partialFixture.Local, partialFixture.Cloud)
            .ConfigureAwait(false);
        partialFixture.Local.Seed(ProgressPath, B);
        partialFixture.Local.Seed(PreferencesPath, PrefsB);
        var partial = await CaptureAtMutationAsync(
            partialFixture.Local,
            partialFixture.Cloud,
            mutation => mutation.Store == "upload-partial-cloud"
                && mutation.Kind == StoreMutationKind.Write
                && mutation.Edge == StoreMutationEdge.After
                && mutation.Path.Equals(
                    ProgressPath,
                    StringComparison.OrdinalIgnoreCase
                ),
            () => RecoverAsync(partialFixture.Local, partialFixture.Cloud)
        ).ConfigureAwait(false);
        ExpectContent(partial.Cloud, ProgressPath, B);
        ExpectContent(partial.Cloud, PreferencesPath, PrefsA);
        ExpectMissing(partial.Cloud, partialFixture.Context.MarkerPath);

        var resumableLocal = partial.Local.Fork("upload-resume-local");
        var resumableCloud = partial.Cloud.Fork("upload-resume-cloud");
        await ExpectRecoveredAsync(resumableLocal, resumableCloud)
            .ConfigureAwait(false);
        ExpectContent(resumableCloud, ProgressPath, B);
        ExpectContent(resumableCloud, PreferencesPath, PrefsB);

        var independentLocal = partial.Local.Fork("upload-independent-local");
        var independentCloud = partial.Cloud.Fork("upload-independent-cloud");
        independentCloud.Seed(PreferencesPath, C);
        var conflict = await RecoverAsync(independentLocal, independentCloud)
            .ConfigureAwait(false);
        ExpectOutcome(conflict, AutomaticSyncOutcome.Conflict);
        ExpectJson(
            independentLocal,
            CloudSyncCoordinator.AutomaticSyncPendingPath
        );
        ExpectContent(independentCloud, PreferencesPath, C);

        var committedFixture = await EstablishedVanillaAsync(
            "upload-committed",
            includePreferences: true
        ).ConfigureAwait(false);
        await ExpectPreparedAsync(committedFixture.Local, committedFixture.Cloud)
            .ConfigureAwait(false);
        committedFixture.Local.Seed(ProgressPath, B);
        committedFixture.Local.Seed(PreferencesPath, PrefsB);
        var committed = await CaptureAtMutationAsync(
            committedFixture.Local,
            committedFixture.Cloud,
            mutation => mutation.Store == "upload-committed-cloud"
                && mutation.Kind == StoreMutationKind.Write
                && mutation.Edge == StoreMutationEdge.After
                && mutation.Path.Equals(
                    committedFixture.Context.MarkerPath,
                    StringComparison.OrdinalIgnoreCase
                ),
            () => RecoverAsync(committedFixture.Local, committedFixture.Cloud)
        ).ConfigureAwait(false);
        ExpectContent(committed.Cloud, ProgressPath, B);
        ExpectContent(
            committed.Cloud,
            committedFixture.Context.MarkerPath,
            committedFixture.Context.SerializeMarker()
        );
        var progressWrites = committed.Cloud.WriteCountFor(ProgressPath);
        await ExpectRecoveredAsync(committed.Local, committed.Cloud)
            .ConfigureAwait(false);
        Expect(
            committed.Cloud.WriteCountFor(ProgressPath) == progressWrites,
            "An already committed interrupted upload rewrote save data."
        );

        foreach (var edge in new[]
                 {
                     StoreMutationEdge.Before,
                     StoreMutationEdge.After,
                 })
        {
            var edgeName = edge == StoreMutationEdge.Before ? "before" : "after";
            var deletion = await EstablishedVanillaAsync(
                $"upload-empty-{edgeName}"
            ).ConfigureAwait(false);
            await ExpectPreparedAsync(deletion.Local, deletion.Cloud)
                .ConfigureAwait(false);
            ((ISaveStore)deletion.Local).DeleteFile(ProgressPath);
            var interrupted = await CaptureAtMutationAsync(
                deletion.Local,
                deletion.Cloud,
                mutation => mutation.Store
                        == $"upload-empty-{edgeName}-cloud"
                    && mutation.Kind == StoreMutationKind.Delete
                    && mutation.Edge == edge
                    && mutation.Path.Equals(
                        ProgressPath,
                        StringComparison.OrdinalIgnoreCase
                    ),
                () => RecoverAsync(deletion.Local, deletion.Cloud),
                $"{edgeName} established delete-to-empty upload tombstone"
            ).ConfigureAwait(false);
            var pending = ExpectJson(
                interrupted.Local,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            );
            ExpectContains(pending, "uploading");
            ExpectMissing(interrupted.Local, ProgressPath);
            ExpectMissing(interrupted.Cloud, deletion.Context.MarkerPath);
            if (edge == StoreMutationEdge.Before)
                ExpectContent(interrupted.Cloud, ProgressPath, A);
            else
                ExpectMissing(interrupted.Cloud, ProgressPath);
            Expect(
                HasTransferBackup(interrupted.Local, ProgressPath),
                "Interrupted empty upload had no destination backup."
            );

            await ExpectRecoveredAsync(interrupted.Local, interrupted.Cloud)
                .ConfigureAwait(false);
            ExpectMissing(interrupted.Local, ProgressPath);
            ExpectMissing(interrupted.Cloud, ProgressPath);
            ExpectContent(
                interrupted.Cloud,
                deletion.Context.MarkerPath,
                deletion.Context.SerializeMarker()
            );
            ExpectBaselineMissing(
                interrupted.Local,
                deletion.Context,
                ProgressPath
            );
            var prepared = await BeginAsync(
                interrupted.Local,
                interrupted.Cloud
            ).ConfigureAwait(false);
            ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
            await ExpectRecoveredAsync(interrupted.Local, interrupted.Cloud)
                .ConfigureAwait(false);
        }
    }

    private static async Task InterruptedPullStatesAsync()
    {
        var fixture = await EstablishedVanillaAsync(
            "pull-partial",
            includePreferences: true
        ).ConfigureAwait(false);
        fixture.Cloud.Seed(ProgressPath, B);
        fixture.Cloud.Seed(PreferencesPath, PrefsB);
        var partial = await CaptureAtMutationAsync(
            fixture.Local,
            fixture.Cloud,
            mutation => mutation.Store == "pull-partial-local"
                && mutation.Kind == StoreMutationKind.Write
                && mutation.Edge == StoreMutationEdge.After
                && mutation.Path.Equals(
                    ProgressPath,
                    StringComparison.OrdinalIgnoreCase
                ),
            () => BeginAsync(fixture.Local, fixture.Cloud)
        ).ConfigureAwait(false);
        ExpectContent(partial.Local, ProgressPath, B);
        ExpectContent(partial.Local, PreferencesPath, PrefsA);
        ExpectJson(partial.Local, CloudSyncCoordinator.AutomaticSyncPendingPath);

        var resumableLocal = partial.Local.Fork("pull-resume-local");
        var resumableCloud = partial.Cloud.Fork("pull-resume-cloud");
        await ExpectRecoveredAsync(resumableLocal, resumableCloud)
            .ConfigureAwait(false);
        ExpectContent(resumableLocal, ProgressPath, B);
        ExpectContent(resumableLocal, PreferencesPath, PrefsB);

        var changedLocal = partial.Local.Fork("pull-independent-local");
        var unchangedCloud = partial.Cloud.Fork("pull-independent-cloud");
        changedLocal.Seed(PreferencesPath, C);
        var conflict = await RecoverAsync(changedLocal, unchangedCloud)
            .ConfigureAwait(false);
        ExpectOutcome(conflict, AutomaticSyncOutcome.Conflict);
        ExpectJson(changedLocal, CloudSyncCoordinator.AutomaticSyncPendingPath);
        ExpectContent(changedLocal, PreferencesPath, C);

        foreach (var edge in new[]
                 {
                     StoreMutationEdge.Before,
                     StoreMutationEdge.After,
                 })
        {
            var edgeName = edge == StoreMutationEdge.Before ? "before" : "after";
            var deletion = await EstablishedVanillaAsync(
                $"pull-empty-{edgeName}"
            ).ConfigureAwait(false);
            await ExpectPreparedAsync(deletion.Local, deletion.Cloud)
                .ConfigureAwait(false);
            ((ISaveStore)deletion.Cloud).DeleteFile(ProgressPath);
            var interrupted = await CaptureAtMutationAsync(
                deletion.Local,
                deletion.Cloud,
                mutation => mutation.Store
                        == $"pull-empty-{edgeName}-local"
                    && mutation.Kind == StoreMutationKind.Delete
                    && mutation.Edge == edge
                    && mutation.Path.Equals(
                        ProgressPath,
                        StringComparison.OrdinalIgnoreCase
                    ),
                () => RecoverAsync(deletion.Local, deletion.Cloud),
                $"{edgeName} established delete-to-empty Pull tombstone"
            ).ConfigureAwait(false);
            var pending = ExpectJson(
                interrupted.Local,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            );
            ExpectContains(pending, "downloading");
            ExpectMissing(interrupted.Cloud, ProgressPath);
            ExpectContent(
                interrupted.Cloud,
                deletion.Context.MarkerPath,
                deletion.Context.SerializeMarker()
            );
            ExpectJson(interrupted.Local, VanillaIncompletePullPath);
            if (edge == StoreMutationEdge.Before)
                ExpectContent(interrupted.Local, ProgressPath, A);
            else
                ExpectMissing(interrupted.Local, ProgressPath);
            Expect(
                HasTransferBackup(interrupted.Local, ProgressPath),
                "Interrupted empty Pull had no destination backup."
            );

            await ExpectRecoveredAsync(interrupted.Local, interrupted.Cloud)
                .ConfigureAwait(false);
            ExpectMissing(interrupted.Local, ProgressPath);
            ExpectMissing(interrupted.Cloud, ProgressPath);
            ExpectMissing(interrupted.Local, VanillaIncompletePullPath);
            ExpectContent(
                interrupted.Cloud,
                deletion.Context.MarkerPath,
                deletion.Context.SerializeMarker()
            );
            ExpectBaselineMissing(
                interrupted.Local,
                deletion.Context,
                ProgressPath
            );
            var prepared = await BeginAsync(
                interrupted.Local,
                interrupted.Cloud
            ).ConfigureAwait(false);
            ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
            await ExpectRecoveredAsync(interrupted.Local, interrupted.Cloud)
                .ConfigureAwait(false);
        }
    }

    private static async Task PersistenceEdgeRestartMatrixAsync()
    {
        var beginEdges = await ObserveBeginMutationsAsync().ConfigureAwait(false);
        ExpectPairedMutationEdges(beginEdges, "before-game preparation");
        for (var index = 0; index < beginEdges.Length; index++)
        {
            var fixture = await EstablishedVanillaAsync(
                $"begin-edge-{index}",
                includePreferences: true
            ).ConfigureAwait(false);
            var captured = await CaptureAtMutationIndexAsync(
                fixture.Local,
                fixture.Cloud,
                beginEdges,
                index,
                () => BeginAsync(fixture.Local, fixture.Cloud)
            ).ConfigureAwait(false);
            ExpectPendingAbsentOrValid(captured.Local);
            await RecoverUntilSettledAsync(captured.Local, captured.Cloud)
                .ConfigureAwait(false);
            var prepared = await BeginAsync(captured.Local, captured.Cloud)
                .ConfigureAwait(false);
            ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
            await ExpectRecoveredAsync(captured.Local, captured.Cloud)
                .ConfigureAwait(false);
        }

        var uploadRecoveryEdges = await ObserveRecoveryMutationsAsync()
            .ConfigureAwait(false);
        ExpectPairedMutationEdges(
            uploadRecoveryEdges,
            "post-game upload recovery"
        );
        for (var index = 0; index < uploadRecoveryEdges.Length; index++)
        {
            var fixture = await EstablishedVanillaAsync(
                $"recovery-edge-{index}",
                includePreferences: true
            ).ConfigureAwait(false);
            await ExpectPreparedAsync(fixture.Local, fixture.Cloud)
                .ConfigureAwait(false);
            fixture.Local.Seed(ProgressPath, B);
            fixture.Local.Seed(PreferencesPath, PrefsB);
            var captured = await CaptureAtMutationIndexAsync(
                fixture.Local,
                fixture.Cloud,
                uploadRecoveryEdges,
                index,
                () => RecoverAsync(fixture.Local, fixture.Cloud)
            ).ConfigureAwait(false);
            ExpectPendingAbsentOrValid(captured.Local);
            await RecoverUntilSettledAsync(captured.Local, captured.Cloud)
                .ConfigureAwait(false);
            ExpectContent(captured.Cloud, ProgressPath, B);
            ExpectContent(captured.Cloud, PreferencesPath, PrefsB);
            ExpectMissing(
                captured.Local,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            );
        }

        var pullRecoveryEdges = await ObservePullRecoveryMutationsAsync()
            .ConfigureAwait(false);
        ExpectPairedMutationEdges(
            pullRecoveryEdges,
            "post-game Pull recovery"
        );
        ExpectPullRecoveryMutationCoverage(pullRecoveryEdges);
        for (var index = 0; index < pullRecoveryEdges.Length; index++)
        {
            var fixture = await EstablishedVanillaAsync(
                $"pull-recovery-edge-{index}",
                includePreferences: true
            ).ConfigureAwait(false);
            await ExpectPreparedAsync(fixture.Local, fixture.Cloud)
                .ConfigureAwait(false);
            fixture.Cloud.Seed(ProgressPath, B);
            ((ISaveStore)fixture.Cloud).DeleteFile(PreferencesPath);
            var captured = await CaptureAtMutationIndexAsync(
                fixture.Local,
                fixture.Cloud,
                pullRecoveryEdges,
                index,
                () => RecoverAsync(fixture.Local, fixture.Cloud)
            ).ConfigureAwait(false);
            ExpectPendingAbsentOrValid(captured.Local);
            await RecoverUntilSettledAsync(captured.Local, captured.Cloud)
                .ConfigureAwait(false);
            ExpectContent(captured.Local, ProgressPath, B);
            ExpectMissing(captured.Local, PreferencesPath);
            ExpectContent(captured.Cloud, ProgressPath, B);
            ExpectMissing(captured.Cloud, PreferencesPath);
            ExpectMissing(captured.Local, VanillaIncompletePullPath);
            ExpectMissing(
                captured.Local,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            );
            Expect(
                HasTransferBackup(captured.Local, ProgressPath)
                    && HasTransferBackup(captured.Local, PreferencesPath),
                $"Pull restart edge {index} completed without destination backups."
            );
        }

        Console.WriteLine(
            $"  persistence edges covered: begin={beginEdges.Length}; "
                + $"upload-recovery={uploadRecoveryEdges.Length}; "
                + $"pull-recovery={pullRecoveryEdges.Length}"
        );
    }

    private static async Task ContextSeparationAndLaunchGateAsync()
    {
        var sharedLocal = Store("same-store-context-local");
        var sharedCloud = Store("same-store-context-cloud");
        var publicContext = VanillaContext(SteamGameBranch.Public);
        var modAContext = ModdedContext(ModFingerprintA);
        sharedLocal.Seed(ProgressPath, B);
        sharedLocal.Seed(ModdedProgressPath, B);
        sharedCloud.Seed(ProgressPath, A);
        sharedCloud.Seed(ModdedProgressPath, A);
        SeedMarker(sharedCloud, publicContext);
        SeedMarker(sharedCloud, modAContext);
        var localBeforeMismatch = SnapshotStore(sharedLocal);
        var cloudBeforeMismatch = SnapshotStore(sharedCloud);
        var localWritesBeforeMismatch = sharedLocal.WriteCount;
        var localDeletesBeforeMismatch = sharedLocal.DeleteCount;
        var cloudWritesBeforeMismatch = sharedCloud.WriteCount;
        var cloudDeletesBeforeMismatch = sharedCloud.DeleteCount;
        var mismatches = new[]
        {
            (
                Namespace: SaveNamespace.Vanilla,
                Runtime: PublicBeta,
                Fingerprint: "",
                Description: "public/beta"
            ),
            (
                Namespace: SaveNamespace.Modded,
                Runtime: SteamGameBranch.Public,
                Fingerprint: ModFingerprintA,
                Description: "modded public/beta"
            ),
            (
                Namespace: SaveNamespace.Modded,
                Runtime: PublicBeta,
                Fingerprint: ModFingerprintB,
                Description: "mod set"
            ),
        };
        foreach (var mismatch in mismatches)
        {
            var blockedMismatch = await ReconcileAsync(
                sharedLocal,
                sharedCloud,
                AutomaticSyncSourceChoice.Local,
                mismatch.Namespace,
                mismatch.Runtime,
                mismatch.Fingerprint
            ).ConfigureAwait(false);
            ExpectOutcome(blockedMismatch, AutomaticSyncOutcome.Conflict);
            Expect(
                blockedMismatch.HasConflict && !blockedMismatch.CanStartGame,
                $"Same-store {mismatch.Description} mismatch allowed play."
            );
            ExpectStoreMatches(
                sharedLocal,
                localBeforeMismatch,
                $"Same-store {mismatch.Description} mismatch mutated Android"
            );
            ExpectStoreMatches(
                sharedCloud,
                cloudBeforeMismatch,
                $"Same-store {mismatch.Description} mismatch mutated Steam"
            );
            Expect(
                sharedLocal.WriteCount == localWritesBeforeMismatch
                    && sharedLocal.DeleteCount == localDeletesBeforeMismatch
                    && sharedCloud.WriteCount == cloudWritesBeforeMismatch
                    && sharedCloud.DeleteCount == cloudDeletesBeforeMismatch,
                $"Same-store {mismatch.Description} mismatch performed a persistence mutation."
            );
            ExpectMissing(
                sharedLocal,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            );
        }

        var local = Store("contexts-local");
        local.Seed(ProgressPath, A);
        local.Seed(ModdedProgressPath, A);
        var contexts = new[]
        {
            VanillaContext(runtime: SteamGameBranch.Public),
            VanillaContext(runtime: PublicBeta),
            ModdedContext(ModFingerprintA),
            ModdedContext(ModFingerprintB),
        };
        var clouds = contexts
            .Select((_, index) => Store($"contexts-cloud-{index}"))
            .ToArray();
        var beforeContents = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        for (var index = 0; index < contexts.Length; index++)
        {
            var context = contexts[index];
            SeedMarker(clouds[index], context);
            var reconciled = await ReconcileAsync(
                local,
                clouds[index],
                AutomaticSyncSourceChoice.Local,
                context.Namespace,
                context.RuntimeIdentity,
                context.ModSetFingerprint
            ).ConfigureAwait(false);
            ExpectOutcome(reconciled, AutomaticSyncOutcome.Synchronized);
            var prepared = await BeginAsync(
                local,
                clouds[index],
                context.Namespace,
                context.RuntimeIdentity,
                context.ModSetFingerprint
            ).ConfigureAwait(false);
            ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
            var beforePath = PendingSnapshotPath(ExpectJson(
                local,
                CloudSyncCoordinator.AutomaticSyncPendingPath
            ));
            beforeContents[beforePath] = ExpectJson(local, beforePath);
            await ExpectRecoveredAsync(local, clouds[index])
                .ConfigureAwait(false);
        }

        var baselinePaths = contexts
            .Select(CloudSyncCoordinator.AutomaticSyncBaselinePath)
            .ToArray();
        var beforePaths = beforeContents.Keys.ToArray();
        Expect(
            baselinePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                == contexts.Length,
            "Runtime or mod-set baseline paths collided."
        );
        Expect(
            beforePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                == contexts.Length,
            "Runtime or mod-set before-game paths collided."
        );
        foreach (var path in baselinePaths.Concat(beforePaths))
            ExpectJson(local, path);

        var publicPrepared = await BeginAsync(
            local,
            clouds[0],
            contexts[0].Namespace,
            contexts[0].RuntimeIdentity,
            contexts[0].ModSetFingerprint
        ).ConfigureAwait(false);
        ExpectOutcome(
            publicPrepared,
            AutomaticSyncOutcome.GameSessionPrepared
        );
        var betaWrites = clouds[1].WriteCount;
        var blocked = await BeginAsync(
            local,
            clouds[1],
            contexts[1].Namespace,
            contexts[1].RuntimeIdentity,
            contexts[1].ModSetFingerprint
        ).ConfigureAwait(false);
        ExpectOutcome(
            blocked,
            AutomaticSyncOutcome.PendingRecoveryRequired
        );
        Expect(!blocked.CanStartGame, "A second context launched over pending work.");
        Expect(
            clouds[1].WriteCount == betaWrites,
            "A second context mutated Steam before pending recovery."
        );
        foreach (var pair in beforeContents)
            ExpectContent(local, pair.Key, pair.Value);
        await ExpectRecoveredAsync(local, clouds[0]).ConfigureAwait(false);
    }

    private static Task ResultSemanticsAsync()
    {
        foreach (var outcome in Enum.GetValues<AutomaticSyncOutcome>())
        {
            var result = new AutomaticSyncResult(outcome, outcome.ToString());
            Expect(
                result.CanStartGame
                    == (outcome == AutomaticSyncOutcome.GameSessionPrepared),
                $"{outcome} has incorrect launch-gate semantics."
            );
            Expect(
                result.HasConflict
                    == (outcome == AutomaticSyncOutcome.Conflict),
                $"{outcome} has incorrect conflict semantics."
            );
        }

        return Task.CompletedTask;
    }

    private static async Task RawByteSnapshotAsync()
    {
        var local = Store("raw-snapshot-local");
        var context = VanillaContext();
        var text = "{\"note\":\"café\"}\r\n\0";
        var content = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(text))
            .ToArray();
        local.SeedBytes(ProgressPath, content);

        var retained = await CloudSyncCoordinator
            .CaptureRetainedLocalSnapshotAsync(
                local,
                context,
                "raw-byte-test",
                CancellationToken.None
            ).ConfigureAwait(false);
        Expect(
            retained.Path == CloudSyncCoordinator.AutomaticSyncSnapshotPath(
                context,
                retained.Sha256
            ),
            "Retained snapshot was not stored at its content-addressed context path."
        );
        var verified = await CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
            local,
            retained.Path,
            context,
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(verified.Version == 2, "New retained snapshot was not version 2.");
        Expect(verified.Coverage == "full", "Stage 3 snapshot was not full coverage.");
        Expect(verified.SourceKind == "local", "Local snapshot source kind was lost.");
        Expect(
            verified.SourceLabel == "raw-byte-test",
            "Local snapshot source label was lost."
        );
        Expect(
            CloudSyncCoordinator.ReadSnapshotFileBytes(verified, ProgressPath)
                .SequenceEqual(content),
            "BOM/CRLF/non-ASCII/NUL bytes did not round-trip exactly."
        );
    }

    private static async Task SnapshotCorruptionIsRejectedAsync()
    {
        var local = Store("snapshot-corruption-local");
        var context = VanillaContext();
        local.Seed(ProgressPath, A);
        var retained = await CloudSyncCoordinator
            .CaptureRetainedLocalSnapshotAsync(
                local,
                context,
                "corruption-test",
                CancellationToken.None
            ).ConfigureAwait(false);
        var validJson = ExpectJson(local, retained.Path);

        var outerCorrupt = local.Fork("snapshot-outer-corrupt");
        outerCorrupt.Seed(retained.Path, validJson + " ");
        await ExpectSnapshotRejectedAsync(() =>
            CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
                outerCorrupt,
                retained.Path,
                context,
                CancellationToken.None
            )
        ).ConfigureAwait(false);

        var document = JsonNode.Parse(validJson)?.AsObject()
            ?? throw new InvalidOperationException("Could not parse retained snapshot JSON.");
        var files = document["Files"]?.AsArray()
            ?? throw new InvalidOperationException("Retained snapshot omitted Files.");
        var progress = files
            .Select(node => node?.AsObject())
            .First(file => string.Equals(
                file?["Path"]?.GetValue<string>(),
                ProgressPath,
                StringComparison.OrdinalIgnoreCase
            )) ?? throw new InvalidOperationException("Retained snapshot omitted progress bytes.");
        progress["ContentBase64"] = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(B)
        );
        var entryCorruptJson = document.ToJsonString();
        var entryCorruptHash = AutomaticSyncHash.Compute(entryCorruptJson);
        var entryCorruptPath = CloudSyncCoordinator.AutomaticSyncSnapshotPath(
            context,
            entryCorruptHash
        );
        local.Seed(entryCorruptPath, entryCorruptJson);
        await ExpectSnapshotRejectedAsync(() =>
            CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
                local,
                entryCorruptPath,
                context,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
    }

    private static async Task RetainedSnapshotPruningAsync()
    {
        var local = Store("snapshot-retention-local");
        var context = VanillaContext();
        var retained = new List<CloudSyncCoordinator.RetainedAutomaticSaveSnapshot>();
        for (var index = 0; index < 6; index++)
        {
            local.Seed(ProgressPath, $"{{\"progress\":{index}}}");
            retained.Add(await CloudSyncCoordinator
                .CaptureRetainedLocalSnapshotAsync(
                    local,
                    context,
                    $"retention-{index}",
                    CancellationToken.None
                ).ConfigureAwait(false));
        }

        await CloudSyncCoordinator.PruneRetainedSnapshotsAsync(
            local,
            context,
            new[] { retained[0].Path },
            CancellationToken.None
        ).ConfigureAwait(false);
        var directory = CloudSyncCoordinator.AutomaticSyncSnapshotDirectory(
            context
        );
        Expect(
            ((ISaveStore)local).GetFilesInDirectory(directory).Length
                == CloudSyncCoordinator.AutomaticSyncRetainedSnapshotLimit + 1,
            "Retention did not keep exactly five unreferenced snapshots plus protected evidence."
        );
        Expect(
            local.Contains(retained[0].Path),
            "Retention pruned explicitly protected snapshot evidence."
        );

        await CloudSyncCoordinator.PruneRetainedSnapshotsAsync(
            local,
            context,
            Array.Empty<string>(),
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(
            ((ISaveStore)local).GetFilesInDirectory(directory).Length
                == CloudSyncCoordinator.AutomaticSyncRetainedSnapshotLimit,
            "Unreferenced retained snapshots were not bounded to five."
        );
        Expect(
            local.Contains(retained[^1].Path),
            "Retention pruned the newest verified snapshot."
        );
    }

    private static async Task LegacyBeforeGameFallbackAsync()
    {
        var fixture = await EstablishedVanillaAsync("legacy-before-game")
            .ConfigureAwait(false);
        var prepared = await BeginAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(prepared, AutomaticSyncOutcome.GameSessionPrepared);
        var pendingPath = CloudSyncCoordinator.AutomaticSyncPendingPath;
        var pendingJson = ExpectJson(fixture.Local, pendingPath);
        var retainedPath = PendingSnapshotPath(pendingJson);
        var retained = await CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
            fixture.Local,
            retainedPath,
            fixture.Context,
            CancellationToken.None
        ).ConfigureAwait(false);
        var retainedJson = ExpectJson(fixture.Local, retainedPath);
        var legacyJson = JsonSerializer.Serialize(new
        {
            Version = 1,
            ContextMarker = retained.ContextMarker,
            Manifest = retained.Manifest,
            Files = retained.Files.Select(file => new
            {
                file.Path,
                Content = CloudSyncCoordinator.DecodeAutomaticSnapshotBytes(
                    CloudSyncCoordinator.ReadSnapshotFileBytes(
                        retained,
                        file.Path
                    ),
                    file.Path
                ),
            }).ToArray(),
        });
        var legacyPath = CloudSyncCoordinator.AutomaticSyncBeforeGamePath(
            fixture.Context
        );
        var contentAddressedLegacyPath =
            CloudSyncCoordinator.AutomaticSyncSnapshotPath(
                fixture.Context,
                AutomaticSyncHash.Compute(legacyJson)
            );
        fixture.Local.Seed(contentAddressedLegacyPath, legacyJson);
        await ExpectSnapshotRejectedAsync(() =>
            CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
                fixture.Local,
                contentAddressedLegacyPath,
                fixture.Context,
                CancellationToken.None
            )
        ).ConfigureAwait(false);

        fixture.Local.Seed(legacyPath, retainedJson);
        await ExpectSnapshotRejectedAsync(() =>
            CloudSyncCoordinator.ReadVerifiedSnapshotAsync(
                fixture.Local,
                legacyPath,
                fixture.Context,
                CancellationToken.None
            )
        ).ConfigureAwait(false);
        fixture.Local.Seed(legacyPath, legacyJson);
        ((ISaveStore)fixture.Local).DeleteFile(retainedPath);

        var pending = JsonNode.Parse(pendingJson)?.AsObject()
            ?? throw new InvalidOperationException("Could not parse pending sync JSON.");
        pending.Remove("BeforeGameSnapshotPath");
        pending["BeforeGameSnapshotSha256"] = AutomaticSyncHash.Compute(
            legacyJson
        );
        fixture.Local.Seed(pendingPath, pending.ToJsonString());

        var recovered = await RecoverAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        ExpectOutcome(recovered, AutomaticSyncOutcome.Synchronized);
        ExpectMissing(fixture.Local, pendingPath);
        ExpectContent(fixture.Local, ProgressPath, A);
        ExpectContent(fixture.Cloud, ProgressPath, A);
    }

    private static async Task ExpectSnapshotRejectedAsync(Func<Task> read)
    {
        try
        {
            await read().ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            return;
        }

        throw new InvalidOperationException(
            "Corrupt snapshot was accepted as verified recovery evidence."
        );
    }

    private static async Task<StoreFixture> EstablishedVanillaAsync(
        string name,
        bool includePreferences = false
    )
    {
        var local = Store($"{name}-local");
        var cloud = Store($"{name}-cloud");
        var context = VanillaContext();
        local.Seed(ProgressPath, A);
        cloud.Seed(ProgressPath, A);
        if (includePreferences)
        {
            local.Seed(PreferencesPath, PrefsA);
            cloud.Seed(PreferencesPath, PrefsA);
        }
        SeedMarker(cloud, context);
        var established = await ReconcileAsync(
            local,
            cloud,
            AutomaticSyncSourceChoice.Local
        ).ConfigureAwait(false);
        ExpectOutcome(established, AutomaticSyncOutcome.Synchronized);
        ExpectJson(
            local,
            CloudSyncCoordinator.AutomaticSyncBaselinePath(context)
        );
        return new StoreFixture(local, cloud, context);
    }

    private static async Task<StoreMutation[]> ObserveBeginMutationsAsync()
    {
        var fixture = await EstablishedVanillaAsync(
            "observe-begin",
            includePreferences: true
        ).ConfigureAwait(false);
        var mutations = new ConcurrentQueue<StoreMutation>();
        fixture.Local.MutationObserved = mutations.Enqueue;
        fixture.Cloud.MutationObserved = mutations.Enqueue;
        try
        {
            await ExpectPreparedAsync(fixture.Local, fixture.Cloud)
                .ConfigureAwait(false);
        }
        finally
        {
            fixture.Local.MutationObserved = null;
            fixture.Cloud.MutationObserved = null;
        }
        return mutations.ToArray();
    }

    private static async Task<StoreMutation[]> ObserveRecoveryMutationsAsync()
    {
        var fixture = await EstablishedVanillaAsync(
            "observe-recovery",
            includePreferences: true
        ).ConfigureAwait(false);
        await ExpectPreparedAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        fixture.Local.Seed(ProgressPath, B);
        fixture.Local.Seed(PreferencesPath, PrefsB);
        var mutations = new ConcurrentQueue<StoreMutation>();
        fixture.Local.MutationObserved = mutations.Enqueue;
        fixture.Cloud.MutationObserved = mutations.Enqueue;
        try
        {
            await ExpectRecoveredAsync(fixture.Local, fixture.Cloud)
                .ConfigureAwait(false);
        }
        finally
        {
            fixture.Local.MutationObserved = null;
            fixture.Cloud.MutationObserved = null;
        }
        return mutations.ToArray();
    }

    private static async Task<StoreMutation[]>
        ObservePullRecoveryMutationsAsync()
    {
        var fixture = await EstablishedVanillaAsync(
            "observe-pull-recovery",
            includePreferences: true
        ).ConfigureAwait(false);
        await ExpectPreparedAsync(fixture.Local, fixture.Cloud)
            .ConfigureAwait(false);
        fixture.Cloud.Seed(ProgressPath, B);
        ((ISaveStore)fixture.Cloud).DeleteFile(PreferencesPath);
        var mutations = new ConcurrentQueue<StoreMutation>();
        fixture.Local.MutationObserved = mutations.Enqueue;
        fixture.Cloud.MutationObserved = mutations.Enqueue;
        try
        {
            await ExpectRecoveredAsync(fixture.Local, fixture.Cloud)
                .ConfigureAwait(false);
        }
        finally
        {
            fixture.Local.MutationObserved = null;
            fixture.Cloud.MutationObserved = null;
        }
        return mutations.ToArray();
    }

    private static async Task<RestartStores> CaptureAtMutationIndexAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        StoreMutation[] canonical,
        int targetIndex,
        Func<Task<AutomaticSyncResult>> operation
    )
    {
        var expected = canonical[targetIndex];
        var expectedOccurrence = canonical
            .Take(targetIndex + 1)
            .Count(candidate => SameMutationShape(candidate, expected));
        var occurrence = 0;
        return await CaptureAtMutationAsync(
            local,
            cloud,
            mutation =>
            {
                if (!SameMutationShape(mutation, expected))
                    return false;
                return Interlocked.Increment(ref occurrence)
                    == expectedOccurrence;
            },
            operation,
            $"{MutationShape(expected)} occurrence {expectedOccurrence}"
        ).ConfigureAwait(false);
    }

    private static async Task<RestartStores> CaptureAtMutationAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        Func<StoreMutation, bool> predicate,
        Func<Task<AutomaticSyncResult>> operation,
        string targetDescription = "requested persistence edge"
    )
    {
        RestartStores? captured = null;
        void Observe(StoreMutation mutation)
        {
            if (captured is not null || !predicate(mutation))
                return;
            captured = new RestartStores(
                local.Fork("restart-local"),
                cloud.Fork("restart-cloud")
            );
            throw new SimulatedProcessTerminationException();
        }

        local.MutationObserved = Observe;
        cloud.MutationObserved = Observe;
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (SimulatedProcessTerminationException)
        {
            // The captured forks are the persisted state at abrupt termination.
        }
        finally
        {
            local.MutationObserved = null;
            cloud.MutationObserved = null;
        }

        Expect(
            captured is not null,
            $"The {targetDescription} was not reached."
        );
        return captured!;
    }

    private static bool SameMutationShape(
        StoreMutation left,
        StoreMutation right
    )
        => MutationShape(left).Equals(
            MutationShape(right),
            StringComparison.OrdinalIgnoreCase
        );

    private static string MutationShape(StoreMutation mutation)
        => $"{StoreRole(mutation.Store)}:{mutation.Kind}:{mutation.Edge}:"
            + $"{StablePath(mutation.Path)}->{StablePath(mutation.DestinationPath)}";

    private static string StoreRole(string store)
        => store.EndsWith("-cloud", StringComparison.OrdinalIgnoreCase)
            || store.Equals("cloud", StringComparison.OrdinalIgnoreCase)
            ? "cloud"
            : "local";

    private static string StablePath(string path)
    {
        var stable = Regex.Replace(
            path ?? "",
            "(?<=transfer-backups/)[0-9]{8}T[0-9]{9}Z-",
            "{backup-time}-",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );
        return Regex.Replace(
            stable,
            "[0-9a-f]{32}",
            "{operation-id}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );
    }

    private static void ExpectPairedMutationEdges(
        StoreMutation[] mutations,
        string scenario
    )
    {
        Expect(mutations.Length > 0, $"{scenario} persisted no state.");
        Expect(
            mutations.Length % 2 == 0,
            $"{scenario} exposed an unpaired persistence edge."
        );
        for (var index = 0; index < mutations.Length; index += 2)
        {
            var before = mutations[index];
            var after = mutations[index + 1];
            Expect(
                before.Edge == StoreMutationEdge.Before
                    && after.Edge == StoreMutationEdge.After
                    && before.Store == after.Store
                    && before.Kind == after.Kind
                    && before.Path.Equals(
                        after.Path,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && before.DestinationPath.Equals(
                        after.DestinationPath,
                        StringComparison.OrdinalIgnoreCase
                    ),
                $"{scenario} mutation edges are not adjacent and paired at {index}."
            );
        }
    }

    private static void ExpectPullRecoveryMutationCoverage(
        StoreMutation[] mutations
    )
    {
        var context = VanillaContext();
        var baselinePath = CloudSyncCoordinator.AutomaticSyncBaselinePath(
            context
        );
        Expect(
            mutations.Any(mutation => StoreRole(mutation.Store) == "local"
                && mutation.Kind is StoreMutationKind.Write
                    or StoreMutationKind.Rename
                && MutationTouchesPath(mutation, VanillaIncompletePullPath)),
            "Pull persistence trace omitted incomplete-marker creation."
        );
        Expect(
            mutations.Any(mutation => StoreRole(mutation.Store) == "local"
                && mutation.Kind == StoreMutationKind.Delete
                && mutation.Path.Equals(
                    VanillaIncompletePullPath,
                    StringComparison.OrdinalIgnoreCase
                )),
            "Pull persistence trace omitted incomplete-marker deletion."
        );
        Expect(
            mutations.Any(mutation => StoreRole(mutation.Store) == "local"
                && mutation.Kind == StoreMutationKind.Write
                && Canonical(mutation.Path).StartsWith(
                    ".sts2-launcher/transfer-backups/",
                    StringComparison.OrdinalIgnoreCase
                )
                && Canonical(mutation.Path).Contains(
                    "/files/profile1/saves/",
                    StringComparison.OrdinalIgnoreCase
                )),
            "Pull persistence trace omitted destination backups."
        );
        Expect(
            mutations.Any(mutation => StoreRole(mutation.Store) == "local"
                && mutation.Kind == StoreMutationKind.Delete
                && mutation.Path.Equals(
                    PreferencesPath,
                    StringComparison.OrdinalIgnoreCase
                )),
            "Pull persistence trace omitted the remote tombstone."
        );
        Expect(
            mutations.Any(mutation => StoreRole(mutation.Store) == "local"
                && MutationTouchesPath(mutation, baselinePath)),
            "Pull persistence trace omitted the baseline commit."
        );
        Expect(
            mutations.Any(mutation => StoreRole(mutation.Store) == "local"
                && mutation.Kind == StoreMutationKind.Delete
                && mutation.Path.Equals(
                    CloudSyncCoordinator.AutomaticSyncPendingPath,
                    StringComparison.OrdinalIgnoreCase
                )),
            "Pull persistence trace omitted pending-record deletion."
        );
    }

    private static bool MutationTouchesPath(
        StoreMutation mutation,
        string path
    )
        => mutation.Path.Equals(path, StringComparison.OrdinalIgnoreCase)
            || mutation.DestinationPath.Equals(
                path,
                StringComparison.OrdinalIgnoreCase
            );

    private static async Task RecoverUntilSettledAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud
    )
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var result = await RecoverAsync(local, cloud).ConfigureAwait(false);
            if (result.Outcome == AutomaticSyncOutcome.Synchronized)
                return;
            Expect(
                result.Outcome == AutomaticSyncOutcome.PendingRecoveryRequired,
                $"Restart recovery stopped as {result.Outcome}: {result.Message}"
            );
        }

        throw new InvalidOperationException(
            "Restart recovery remained pending after three unchanged retries."
        );
    }

    private static void ExpectPendingAbsentOrValid(
        InMemoryCloudSaveStore local
    )
    {
        if (!local.Contains(CloudSyncCoordinator.AutomaticSyncPendingPath))
            return;
        ExpectJson(local, CloudSyncCoordinator.AutomaticSyncPendingPath);
    }

    private static Task<AutomaticSyncResult> ReconcileAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        AutomaticSyncSourceChoice? sourceChoice = null,
        SaveNamespace saveNamespace = SaveNamespace.Vanilla,
        string runtime = SteamGameBranch.Public,
        string modFingerprint = ""
    )
        => CloudSyncCoordinator.ReconcileAutomaticSyncAsync(
            local,
            cloud,
            saveNamespace,
            runtime,
            modFingerprint,
            sourceChoice,
            Progress(),
            CancellationToken.None
        );

    private static Task<AutomaticSyncResult> BeginAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud,
        SaveNamespace saveNamespace = SaveNamespace.Vanilla,
        string runtime = SteamGameBranch.Public,
        string modFingerprint = ""
    )
        => CloudSyncCoordinator.BeginAutomaticGameSessionAsync(
            local,
            cloud,
            saveNamespace,
            runtime,
            modFingerprint,
            Progress(),
            CancellationToken.None
        );

    private static Task<AutomaticSyncResult> RecoverAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud
    )
        => CloudSyncCoordinator.RecoverAutomaticSyncAsync(
            local,
            cloud,
            Progress(),
            CancellationToken.None
        );

    private static async Task ExpectPreparedAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud
    )
    {
        var result = await BeginAsync(local, cloud).ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.GameSessionPrepared);
    }

    private static async Task ExpectRecoveredAsync(
        InMemoryCloudSaveStore local,
        InMemoryCloudSaveStore cloud
    )
    {
        var result = await RecoverAsync(local, cloud).ConfigureAwait(false);
        ExpectOutcome(result, AutomaticSyncOutcome.Synchronized);
        ExpectMissing(local, CloudSyncCoordinator.AutomaticSyncPendingPath);
    }

    private static CloudOperationProgressTracker Progress()
        => new(CloudOperationKind.Push);

    private static SaveContext VanillaContext(
        string runtime = SteamGameBranch.Public
    )
        => SaveContext.Create(
            SteamId64,
            SaveNamespace.Vanilla,
            runtime,
            ""
        );

    private static SaveContext ModdedContext(string fingerprint)
        => SaveContext.Create(
            SteamId64,
            SaveNamespace.Modded,
            PublicBeta,
            fingerprint
        );

    private static InMemoryCloudSaveStore Store(string name)
        => new(name, SteamId64);

    private static void SeedMarker(
        InMemoryCloudSaveStore cloud,
        SaveContext context
    )
        => cloud.Seed(context.MarkerPath, context.SerializeMarker());

    private static bool HasTransferBackup(
        InMemoryCloudSaveStore local,
        string path
    )
    {
        var suffix = "/files/" + Canonical(path);
        return local.Paths.Any(candidate =>
            Canonical(candidate).StartsWith(
                ".sts2-launcher/transfer-backups/",
                StringComparison.OrdinalIgnoreCase
            )
            && Canonical(candidate).EndsWith(
                suffix,
                StringComparison.OrdinalIgnoreCase
            )
        );
    }

    private static Dictionary<string, string> SnapshotStore(
        InMemoryCloudSaveStore store
    )
    {
        var snapshot = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var path in store.Paths)
        {
            Expect(
                store.TryReadSeeded(path, out var content),
                $"Could not snapshot fake-store path {path}."
            );
            snapshot[path] = content;
        }
        return snapshot;
    }

    private static void ExpectStoreMatches(
        InMemoryCloudSaveStore store,
        IReadOnlyDictionary<string, string> expected,
        string scenario
    )
    {
        var actual = SnapshotStore(store);
        Expect(
            actual.Count == expected.Count
                && expected.All(pair => actual.TryGetValue(
                    pair.Key,
                    out var content
                ) && content.Equals(pair.Value, StringComparison.Ordinal)),
            $"{scenario} store contents."
        );
    }

    private static void ExpectBaselineMissing(
        InMemoryCloudSaveStore local,
        SaveContext context,
        params string[] paths
    )
    {
        var baseline = ExpectJson(
            local,
            CloudSyncCoordinator.AutomaticSyncBaselinePath(context)
        );
        using var document = JsonDocument.Parse(baseline);
        foreach (var manifestName in new[]
                 {
                     "LocalManifest",
                     "RemoteManifest",
                 })
        {
            var entries = document.RootElement
                .GetProperty(manifestName)
                .GetProperty("Entries");
            foreach (var path in paths)
            {
                var matches = entries.EnumerateArray()
                    .Where(entry => entry.GetProperty("Path")
                        .GetString()?.Equals(
                            path,
                            StringComparison.OrdinalIgnoreCase
                        ) == true)
                    .ToArray();
                Expect(
                    matches.Length == 1
                        && !matches[0].GetProperty("Exists").GetBoolean()
                        && string.IsNullOrEmpty(
                            matches[0].GetProperty("Sha256").GetString()
                        ),
                    $"{manifestName} did not persist {path} as missing."
                );
            }
        }
    }

    private static string ExpectJson(
        InMemoryCloudSaveStore store,
        string path
    )
    {
        Expect(
            store.TryReadSeeded(path, out var content),
            $"Expected JSON state at {path}."
        );
        try
        {
            using var _ = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Persisted state is not complete JSON at {path}.",
                ex
            );
        }
        return content;
    }

    private static void ExpectContent(
        InMemoryCloudSaveStore store,
        string path,
        string expected
    )
        => Expect(
            store.TryReadSeeded(path, out var actual)
                && actual.Equals(expected, StringComparison.Ordinal),
            $"Expected exact content at {path}."
        );

    private static void ExpectMissing(
        InMemoryCloudSaveStore store,
        string path
    )
        => Expect(!store.Contains(path), $"Expected {path} to be absent.");

    private static void ExpectOutcome(
        AutomaticSyncResult result,
        AutomaticSyncOutcome expected
    )
        => Expect(
            result.Outcome == expected,
            $"Expected automatic outcome {expected}; got {result.Outcome}: {result.Message}"
        );

    private static void ExpectContains(string actual, string expected)
        => Expect(
            actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            $"Expected persisted state to contain '{expected}'."
        );

    private static void ExpectSnapshotContent(
        string snapshotJson,
        string path,
        string expectedContent
    )
    {
        using var document = JsonDocument.Parse(snapshotJson);
        var version = document.RootElement.GetProperty("Version").GetInt32();
        var files = document.RootElement.GetProperty("Files");
        foreach (var file in files.EnumerateArray())
        {
            if (file.GetProperty("Path").GetString()?.Equals(
                    path,
                    StringComparison.OrdinalIgnoreCase
                ) == true)
            {
                var actual = version == 1
                    ? file.GetProperty("Content").GetString()
                    : CloudSyncCoordinator.DecodeAutomaticSnapshotBytes(
                        Convert.FromBase64String(
                            file.GetProperty("ContentBase64").GetString() ?? ""
                        ),
                        path
                    );
                Expect(
                    actual == expectedContent,
                    $"Before-game snapshot content changed for {path}."
                );
                return;
            }
        }

        throw new InvalidOperationException(
            $"Before-game snapshot omitted {path}."
        );
    }

    private static string PendingSnapshotPath(string pendingJson)
    {
        using var document = JsonDocument.Parse(pendingJson);
        var path = document.RootElement
            .GetProperty("BeforeGameSnapshotPath")
            .GetString();
        Expect(
            !string.IsNullOrWhiteSpace(path),
            "Pending automatic sync did not retain its immutable snapshot path."
        );
        return path!;
    }

    private static string Canonical(string path)
        => path.Replace('\\', '/').Trim('/');

    private static async Task RunAsync(string name, Func<Task> test)
    {
        await test().ConfigureAwait(false);
        _passed++;
        Console.WriteLine($"PASS: {name}");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record StoreFixture(
        InMemoryCloudSaveStore Local,
        InMemoryCloudSaveStore Cloud,
        SaveContext Context
    );

    private sealed record RestartStores(
        InMemoryCloudSaveStore Local,
        InMemoryCloudSaveStore Cloud
    );

    private sealed class SimulatedProcessTerminationException : Exception
    {
    }
}
