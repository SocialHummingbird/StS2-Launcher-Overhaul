#nullable enable

using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal static class Program
{
    private const ulong AccountA = InMemoryCloudSaveStore.DefaultSteamId64;
    private const ulong AccountB = AccountA + 1;
    private const ulong AccountC = AccountA + 2;
    private const string ProgressPath = "profile1/saves/progress.save";
    private const string ModdedProgressPath =
        "modded/profile1/saves/progress.save";
    private static int _passed;

    private static async Task Main()
    {
        await RunAsync(
            "unique exact Stage 3 evidence infers account and quarantines foreign backup",
            UniqueStage3AccountInferenceAsync
        );
        await RunAsync(
            "multiple Stage 3 accounts remain unknown instead of guessing",
            MultipleStage3AccountsRemainUnknownAsync
        );
        await RunAsync(
            "legacy Stage 3 version 1 snapshot upgrades to verified version 2",
            LegacyStage3SnapshotUpgradesAsync
        );
        await RunAsync(
            "live sidecars are immutable partial imports and dedupe by payload",
            LiveSidecarsArePartialAndDeduplicatedAsync
        );
        await RunAsync(
            "external Current, History, and strict bak sources stay untouched",
            ExternalSourcesAreBoundedAndSettingsAreExportOnlyAsync
        );
        await RunAsync(
            "legacy manual Pull is unknown and invalid Stage 2 context is quarantined",
            ManualPullAndInvalidContextAsync
        );

        Console.WriteLine(
            $"Legacy save recovery scanner probe passed {_passed}/6 scenarios."
        );
    }

    private static async Task UniqueStage3AccountInferenceAsync()
    {
        var local = new InMemoryCloudSaveStore("scanner-inference");
        local.Seed(ProgressPath, "{\"account\":\"a\"}");
        var accountA = VanillaContext(AccountA);
        _ = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            accountA,
            "account-a-stage3",
            CancellationToken.None
        ).ConfigureAwait(false);

        var foreign = VanillaContext(AccountB);
        const string operation = "20260806T120000000Z-foreign";
        var contextPath =
            $".sts2-launcher/transfer-backups/{operation}/context.json";
        var backupPath =
            $".sts2-launcher/transfer-backups/{operation}/files/{ProgressPath}";
        var settingsPath =
            $".sts2-launcher/transfer-backups/{operation}/files/settings.save";
        local.Seed(contextPath, foreign.SerializeMarker());
        local.Seed(backupPath, "{\"account\":\"b\"}");
        local.Seed(settingsPath, "{\"volume\":0.5}");
        var before = ReadBytes(local, backupPath);

        var result = await ScanAsync(local, knownSteamId64: null)
            .ConfigureAwait(false);
        Expect(result.KnownSelectedSteamId64 == AccountA);
        var stage3 = result.Candidates.Single(candidate =>
            candidate.SourceKind
                == CloudSyncCoordinator.LegacyRecoverySourceKind.Stage3Snapshot
                && candidate.Imported
        );
        Expect(
            stage3.Classification
                == CloudSyncCoordinator.LegacyRecoveryClassification
                    .ExactSelectedContext
        );
        Expect(stage3.ExactSteamId64 == AccountA);
        var quarantined = result.Candidates.Single(candidate =>
            candidate.SourceKind
                == CloudSyncCoordinator.LegacyRecoverySourceKind
                    .Stage2TransferBackup
                && candidate.Imported
        );
        Expect(
            quarantined.Classification
                == CloudSyncCoordinator.LegacyRecoveryClassification
                    .QuarantinedForeignAccount
        );
        Expect(!quarantined.CanRestore);
        Expect(result.ExportOnlyFiles.Any(file =>
            string.Equals(file.SourcePath, settingsPath, StringComparison.Ordinal)
        ));
        Expect(quarantined.SourcePaths.All(path => !string.Equals(
            path,
            settingsPath,
            StringComparison.Ordinal
        )));
        Expect(quarantined.RecoverySnapshotPath.StartsWith(
            CloudSyncCoordinator.RecoveryQuarantineDirectory + "/",
            StringComparison.Ordinal
        ));
        ExpectBytes(local, backupPath, before);
        ExpectSeeded(local, settingsPath, "{\"volume\":0.5}");
        _ = await CloudSyncCoordinator.ReadVerifiedImportedSnapshotAsync(
            local,
            quarantined.RecoverySnapshotPath,
            foreign,
            CancellationToken.None
        ).ConfigureAwait(false);
    }

    private static async Task MultipleStage3AccountsRemainUnknownAsync()
    {
        var local = new InMemoryCloudSaveStore("scanner-ambiguous-account");
        local.Seed(ProgressPath, "{\"progress\":10}");
        foreach (var account in new[] { AccountA, AccountB })
        {
            _ = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
                local,
                VanillaContext(account),
                $"stage3-{account}",
                CancellationToken.None
            ).ConfigureAwait(false);
        }
        const string operation = "20260806T120100000Z-third";
        local.Seed(
            $".sts2-launcher/transfer-backups/{operation}/context.json",
            VanillaContext(AccountC).SerializeMarker()
        );
        local.Seed(
            $".sts2-launcher/transfer-backups/{operation}/files/{ProgressPath}",
            "{\"progress\":8}"
        );

        var result = await ScanAsync(local, knownSteamId64: null)
            .ConfigureAwait(false);
        Expect(result.KnownSelectedSteamId64 is null);
        var exactCandidates = result.Candidates
            .Where(candidate => candidate.Imported)
            .Where(candidate =>
                candidate.Provenance.SteamId64.Knowledge
                    == CloudSyncCoordinator.LegacyRecoveryProvenanceKnowledge.Exact
            )
            .ToArray();
        Expect(exactCandidates.Length == 3);
        Expect(exactCandidates.All(candidate =>
            candidate.Provenance.SteamId64.MatchesSelection is null
        ));
        Expect(exactCandidates.All(candidate =>
            candidate.Classification
                == CloudSyncCoordinator.LegacyRecoveryClassification
                    .ExactOtherContext
        ));
        Expect(exactCandidates.All(candidate => candidate.CanRestore));
        Expect(exactCandidates.Select(candidate => candidate.ExactSteamId64)
            .ToHashSet().SetEquals(new ulong?[] { AccountA, AccountB, AccountC }));
    }

    private static async Task LiveSidecarsArePartialAndDeduplicatedAsync()
    {
        var local = new InMemoryCloudSaveStore("scanner-sidecars");
        var vanilla = Encoding.UTF8.GetBytes("{\"regent\":8}");
        var modded = Encoding.UTF8.GetBytes("{\"ironclad\":10}");
        var backupPath = $"{ProgressPath}.backup";
        var tempPath = $"{ProgressPath}.tmp";
        var cloudTempPath =
            $"{ModdedProgressPath}.sts2-cloud-0123456789abcdef0123456789abcdef.tmp";
        local.SeedBytes(backupPath, vanilla);
        local.SeedBytes(tempPath, vanilla);
        local.SeedBytes(cloudTempPath, modded);

        var result = await ScanAsync(local, AccountA).ConfigureAwait(false);
        Expect(result.KnownSelectedSteamId64 == AccountA);
        var sidecars = result.Candidates
            .Where(candidate =>
                candidate.SourceKind
                    == CloudSyncCoordinator.LegacyRecoverySourceKind.LiveSidecar
            )
            .Where(candidate => candidate.Imported)
            .ToArray();
        Expect(sidecars.Length == 2);
        Expect(result.DuplicateCandidatesSkipped == 1);
        Expect(sidecars.All(candidate => candidate.Coverage == "partial"));
        Expect(sidecars.All(candidate => candidate.OverwriteOnly));
        Expect(sidecars.All(candidate =>
            candidate.Classification
                == CloudSyncCoordinator.LegacyRecoveryClassification.UnknownContext
        ));
        Expect(sidecars.All(candidate => candidate.CanRestore));
        Expect(sidecars.All(candidate => candidate.ExactSteamId64 is null));
        ExpectBytes(local, backupPath, vanilla);
        ExpectBytes(local, tempPath, vanilla);
        ExpectBytes(local, cloudTempPath, modded);
        foreach (var candidate in sidecars)
        {
            var snapshot = await CloudSyncCoordinator
                .ReadVerifiedImportedSnapshotAsync(
                    local,
                    candidate.RecoverySnapshotPath,
                    CancellationToken.None
                ).ConfigureAwait(false);
            Expect(snapshot.Version == 2);
            Expect(snapshot.Coverage == "partial");
        }
    }

    private static async Task LegacyStage3SnapshotUpgradesAsync()
    {
        var local = new InMemoryCloudSaveStore("scanner-stage3-v1");
        var rawProgress = Encoding.UTF8.GetBytes("{\"legacy\":true}\r\n");
        local.SeedBytes(ProgressPath, rawProgress);
        var retained = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            VanillaContext(AccountA),
            "temporary-v2-source",
            CancellationToken.None
        ).ConfigureAwait(false);
        var legacy = new CloudSyncCoordinator.AutomaticSaveSnapshot
        {
            Version = 1,
            ContextMarker = retained.Snapshot.ContextMarker,
            Manifest = retained.Snapshot.Manifest,
            Files = retained.Snapshot.Files.Select(file =>
            {
                var bytes = CloudSyncCoordinator.ReadSnapshotFileBytes(
                    retained.Snapshot,
                    file.Path
                );
                return new CloudSyncCoordinator.AutomaticSaveContentEntry
                {
                    Path = file.Path,
                    Content = CloudSyncCoordinator.DecodeAutomaticSnapshotBytes(
                        bytes,
                        file.Path
                    ),
                };
            }).ToList(),
        };
        ((ISaveStore)local).DeleteFile(retained.Path);
        const string legacyPath =
            ".sts2-launcher/automatic-sync/before-game.json";
        local.Seed(legacyPath, JsonSerializer.Serialize(legacy));

        var result = await ScanAsync(local, AccountA).ConfigureAwait(false);
        var candidate = result.Candidates.Single(item =>
            item.SourceKind
                == CloudSyncCoordinator.LegacyRecoverySourceKind.Stage3Snapshot
                && item.Imported
        );
        Expect(candidate.Coverage == "full");
        Expect(!candidate.OverwriteOnly);
        var imported = await CloudSyncCoordinator.ReadVerifiedImportedSnapshotAsync(
            local,
            candidate.RecoverySnapshotPath,
            VanillaContext(AccountA),
            CancellationToken.None
        ).ConfigureAwait(false);
        Expect(imported.Version == 2);
        var importedProgress = CloudSyncCoordinator.ReadSnapshotFileBytes(
            imported,
            ProgressPath
        );
        Expect(importedProgress.AsSpan().SequenceEqual(rawProgress));
        ExpectSeeded(local, legacyPath, JsonSerializer.Serialize(legacy));
    }

    private static async Task ExternalSourcesAreBoundedAndSettingsAreExportOnlyAsync()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"sts2-legacy-scanner-{Guid.NewGuid():N}"
        );
        try
        {
            var currentProgress = Path.Combine(
                root,
                "Current",
                "profile1",
                "saves",
                "progress.save"
            );
            var settings = Path.Combine(root, "Current", "settings.save");
            var historyProgress = Path.Combine(
                root,
                "History",
                "20260806T1200000000000Z",
                "modded",
                "profile1",
                "saves",
                "progress.save"
            );
            var timestamped = Path.Combine(
                root,
                "profile1",
                "progress.save.1750000000.local-pre-pull.bak"
            );
            var rejected = Path.Combine(
                root,
                "profile1",
                "notes.save.1750000000.local-pre-pull.bak"
            );
            WriteFile(currentProgress, "{\"source\":\"current\"}");
            WriteFile(settings, "{\"volume\":0.5}");
            WriteFile(historyProgress, "{\"source\":\"history\"}");
            WriteFile(timestamped, "{\"source\":\"bak\"}");
            WriteFile(rejected, "{\"source\":\"reject\"}");

            var local = new InMemoryCloudSaveStore("scanner-external");
            var result = await CloudSyncCoordinator.ScanAndImportLegacySavesAsync(
                local,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                AccountA,
                root,
                CancellationToken.None
            ).ConfigureAwait(false);
            var kinds = result.Candidates
                .Where(candidate => candidate.Imported)
                .Select(candidate => candidate.SourceKind)
                .ToHashSet();
            Expect(kinds.Contains(
                CloudSyncCoordinator.LegacyRecoverySourceKind.ExternalCurrent
            ));
            Expect(kinds.Contains(
                CloudSyncCoordinator.LegacyRecoverySourceKind.ExternalHistory
            ));
            Expect(kinds.Contains(
                CloudSyncCoordinator.LegacyRecoverySourceKind
                    .ExternalTimestampedBackup
            ));
            Expect(result.ExportOnlyFiles.Count == 1);
            Expect(string.Equals(
                result.ExportOnlyFiles[0].SourcePath,
                settings,
                StringComparison.OrdinalIgnoreCase
            ));
            Expect(result.Candidates.All(candidate =>
                candidate.SourcePaths.All(path => !string.Equals(
                    path,
                    settings,
                    StringComparison.OrdinalIgnoreCase
                ))
            ));
            Expect(File.ReadAllText(currentProgress) == "{\"source\":\"current\"}");
            Expect(File.ReadAllText(historyProgress) == "{\"source\":\"history\"}");
            Expect(File.ReadAllText(timestamped) == "{\"source\":\"bak\"}");
            Expect(File.ReadAllText(rejected) == "{\"source\":\"reject\"}");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static async Task ManualPullAndInvalidContextAsync()
    {
        var local = new InMemoryCloudSaveStore("scanner-invalid");
        const string manualGeneration = "20260806T120000000Z";
        var manualPath =
            $".launcher_backups/manual-pull/{manualGeneration}/{ModdedProgressPath}";
        local.Seed(manualPath, "{\"manual\":true}");

        const string operation = "20260806T120200000Z-invalid";
        var contextPath =
            $".sts2-launcher/transfer-backups/{operation}/context.json";
        var backupPath =
            $".sts2-launcher/transfer-backups/{operation}/files/{ProgressPath}";
        local.Seed(contextPath, "not-json");
        local.Seed(backupPath, "{\"salvage\":true}");

        var result = await ScanAsync(local, AccountA).ConfigureAwait(false);
        var manual = result.Candidates.Single(candidate =>
            candidate.SourceKind
                == CloudSyncCoordinator.LegacyRecoverySourceKind.ManualPullBackup
                && candidate.Imported
        );
        Expect(
            manual.Provenance.SaveNamespace.Knowledge
                == CloudSyncCoordinator.LegacyRecoveryProvenanceKnowledge.Exact
        );
        Expect(manual.Provenance.SaveNamespace.Value == "modded");
        Expect(
            manual.Provenance.RuntimeIdentity.Knowledge
                == CloudSyncCoordinator.LegacyRecoveryProvenanceKnowledge.Unknown
        );
        var invalid = result.Candidates.Single(candidate =>
            candidate.SourceKind
                == CloudSyncCoordinator.LegacyRecoverySourceKind
                    .Stage2TransferBackup
                && candidate.Imported
        );
        Expect(
            invalid.Classification
                == CloudSyncCoordinator.LegacyRecoveryClassification.Invalid
        );
        Expect(!invalid.CanRestore);
        Expect(invalid.RecoverySnapshotPath.StartsWith(
            CloudSyncCoordinator.RecoveryQuarantineDirectory + "/",
            StringComparison.Ordinal
        ));
        ExpectSeeded(local, contextPath, "not-json");
        ExpectSeeded(local, backupPath, "{\"salvage\":true}");
        ExpectSeeded(local, manualPath, "{\"manual\":true}");
    }

    private static Task<CloudSyncCoordinator.LegacySaveRecoveryScanResult>
        ScanAsync(
            InMemoryCloudSaveStore local,
            ulong? knownSteamId64
        )
        => CloudSyncCoordinator.ScanAndImportLegacySavesAsync(
            local,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            knownSteamId64,
            externalSavesRoot: null,
            CancellationToken.None
        );

    private static SaveContext VanillaContext(ulong steamId64)
        => SaveContext.Create(
            steamId64,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            ""
        );

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static byte[] ReadBytes(
        InMemoryCloudSaveStore store,
        string path
    )
    {
        Expect(store.TryReadSeededBytes(path, out var content));
        return content;
    }

    private static void ExpectBytes(
        InMemoryCloudSaveStore store,
        string path,
        byte[] expected
    )
        => Expect(ReadBytes(store, path).AsSpan().SequenceEqual(expected));

    private static void ExpectSeeded(
        InMemoryCloudSaveStore store,
        string path,
        string expected
    )
        => Expect(
            store.TryReadSeeded(path, out var content)
                && string.Equals(content, expected, StringComparison.Ordinal)
        );

    private static async Task RunAsync(string name, Func<Task> scenario)
    {
        try
        {
            await scenario().ConfigureAwait(false);
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"FAIL {name}: {ex.Message}",
                ex
            );
        }
    }

    private static void Expect(
        bool condition,
        [System.Runtime.CompilerServices.CallerArgumentExpression(
            nameof(condition)
        )]
        string expression = ""
    )
    {
        if (!condition)
            throw new InvalidOperationException(expression);
    }
}
