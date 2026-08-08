using CloudSyncProductionPathProbe;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace LocalSaveRecoveryProductionPathProbe;

internal static class Program
{
    private const string ProfilePath = "profile.save";
    private const string ProgressPath = "profile1/saves/progress.save";
    private const string ProgressBackupPath = $"{ProgressPath}.backup";
    private const string HistoryPath =
        "profile1/saves/history/20260725.run";
    private const string LauncherTransferBackupPath =
        ".sts2-launcher/transfer-backups/operation/profile1/saves/progress.save";
    private static readonly string[] CandidatePaths =
    {
        ProfilePath,
        ProgressPath,
        ProgressBackupPath,
        HistoryPath,
        LauncherTransferBackupPath,
        $"{ProgressPath}.tmp",
    };
    private static int _passed;

    private static async Task Main()
    {
        var tracePath = Path.Combine(
            Path.GetTempPath(),
            $"sts2-local-save-backup-{Guid.NewGuid():N}.log"
        );
        Environment.SetEnvironmentVariable(
            "STS2_BOOTSTRAP_TRACE_FILE",
            tracePath
        );
        try
        {
            await RunAsync(
                "eligible saves mirror into an isolated tree",
                MirrorsEligibleSavesAsync
            );
            await RunAsync(
                "transfer tombstones remain deleted during backup refresh",
                TombstonedSavesStayDeletedAsync
            );
            await RunAsync(
                "newer local saves replace the mirror and archive old bytes",
                NewerLocalSaveIsArchivedAsync
            );
            await RunAsync(
                "corrupt and partial mirror files never restore local saves",
                CorruptMirrorsStayBackupOnlyAsync
            );
            await RunAsync(
                "local mirror refresh preserves exact raw bytes",
                RawBytesRoundTripAsync
            );

            Console.WriteLine(
                $"Local save backup-only production-path probe passed {_passed}/5 scenarios."
            );
            Console.WriteLine(
                "Mutation audit: temporary directories and in-memory stores only; "
                    + "no credentials, network store, hardware, Steam Cloud operation, "
                    + "or automatic save restoration was used."
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                "STS2_BOOTSTRAP_TRACE_FILE",
                null
            );
            if (File.Exists(tracePath))
                File.Delete(tracePath);
        }
    }

    private static Task MirrorsEligibleSavesAsync(string root)
    {
        var local = new InMemoryCloudSaveStore("mirror-local");
        var cloud = UntouchedCloud();
        local.Seed(ProfilePath, "{\"profile\":\"local\"}");
        local.Seed(ProgressPath, "{\"progress\":1}");
        local.Seed(HistoryPath, "{\"run\":1}");
        local.Seed(LauncherTransferBackupPath, "{\"transfer-backup\":1}");
        local.Seed($"{ProgressPath}.tmp", "partial");

        var result = Refresh(local, root);

        Expect(result.Completion == LocalBackupRefreshCompletion.Success);
        Expect(result.Mirrored >= 3);
        ExpectMirror(root, ProfilePath, "{\"profile\":\"local\"}");
        ExpectMirror(root, ProgressPath, "{\"progress\":1}");
        ExpectMirror(root, HistoryPath, "{\"run\":1}");
        Expect(!File.Exists(MirrorPath(root, LauncherTransferBackupPath)));
        Expect(!File.Exists(MirrorPath(root, $"{ProgressPath}.tmp")));
        ExpectCloudUntouched(cloud);
        return Task.CompletedTask;
    }

    private static Task TombstonedSavesStayDeletedAsync(string root)
    {
        var local = new InMemoryCloudSaveStore("tombstone-local");
        var cloud = UntouchedCloud();
        const string profile = "{\"profile\":\"archived\"}";
        const string progress = "{\"progress\":2}";
        const string progressBackup = "{\"progress\":1}";
        local.Seed(ProfilePath, profile);
        local.Seed(ProgressPath, progress);
        local.Seed(ProgressBackupPath, progressBackup);
        Refresh(local, root);

        ((ISaveStore)local).DeleteFile(ProfilePath);
        ((ISaveStore)local).DeleteFile(ProgressPath);
        ((ISaveStore)local).DeleteFile(ProgressBackupPath);
        var result = Refresh(local, root);

        Expect(!((ISaveStore)local).FileExists(ProfilePath));
        Expect(!((ISaveStore)local).FileExists(ProgressPath));
        Expect(!((ISaveStore)local).FileExists(ProgressBackupPath));
        ExpectMirror(root, ProfilePath, profile);
        ExpectMirror(root, ProgressPath, progress);
        ExpectMirror(root, ProgressBackupPath, progressBackup);
        ExpectCloudUntouched(cloud);
        return Task.CompletedTask;
    }

    private static Task NewerLocalSaveIsArchivedAsync(string root)
    {
        var local = new InMemoryCloudSaveStore("newer-local");
        var cloud = UntouchedCloud();
        const string oldContent = "{\"progress\":\"old\"}";
        const string newContent = "{\"progress\":\"new\"}";
        local.Seed(ProgressPath, oldContent, DateTimeOffset.UtcNow.AddHours(-1));
        Refresh(local, root);

        local.Seed(ProgressPath, newContent, DateTimeOffset.UtcNow.AddHours(1));
        var result = Refresh(local, root);

        Expect(result.Archived == 1);
        ExpectSeeded(local, ProgressPath, newContent);
        ExpectMirror(root, ProgressPath, newContent);
        var archived = Directory
            .EnumerateFiles(
                Path.Combine(root, LocalSaveBackupPlan.HistoryDirectoryName),
                Path.GetFileName(ProgressPath),
                SearchOption.AllDirectories
            )
            .Single();
        Expect(File.ReadAllText(archived) == oldContent);
        ExpectCloudUntouched(cloud);
        return Task.CompletedTask;
    }

    private static Task CorruptMirrorsStayBackupOnlyAsync(string root)
    {
        var local = new InMemoryCloudSaveStore("corrupt-mirror-local");
        var cloud = UntouchedCloud();
        var partialPath =
            $"{MirrorPath(root, ProgressPath)}.sts2-cloud-interrupted.tmp";
        var corruptPath = MirrorPath(root, ProfilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(partialPath)!);
        File.WriteAllText(partialPath, "{\"progress\":\"partial\"}");
        Directory.CreateDirectory(Path.GetDirectoryName(corruptPath)!);
        File.WriteAllText(corruptPath, "truncated-save");

        var result = Refresh(local, root);

        Expect(!((ISaveStore)local).FileExists(ProgressPath));
        Expect(!((ISaveStore)local).FileExists(ProfilePath));
        Expect(File.Exists(partialPath));
        Expect(File.ReadAllText(corruptPath) == "truncated-save");
        ExpectCloudUntouched(cloud);
        return Task.CompletedTask;
    }

    private static Task RawBytesRoundTripAsync(string root)
    {
        var local = new InMemoryCloudSaveStore("raw-byte-local");
        var bytes = new byte[] { 0xef, 0xbb, 0xbf }
            .Concat(System.Text.Encoding.UTF8.GetBytes("{\"progress\":3}\r\n"))
            .Concat(new byte[] { 0 })
            .ToArray();
        local.SeedBytes(ProgressPath, bytes);

        var result = Refresh(local, root);

        Expect(result.Completion == LocalBackupRefreshCompletion.Success);
        Expect(File.ReadAllBytes(MirrorPath(root, ProgressPath)).SequenceEqual(bytes));
        return Task.CompletedTask;
    }

    private static LocalBackupRefreshResult Refresh(
        ISaveStore local,
        string root,
        CancellationToken cancellationToken = default
    )
        => CloudSyncCoordinator.RefreshLocalBackup(
            local,
            root,
            CandidatePaths,
            cancellationToken
        );

    private static InMemoryCloudSaveStore UntouchedCloud()
    {
        var cloud = new InMemoryCloudSaveStore("untouched-cloud");
        cloud.Seed(ProgressPath, "{\"cloud\":\"unchanged\"}");
        return cloud;
    }

    private static void ExpectCloudUntouched(
        InMemoryCloudSaveStore cloud
    )
    {
        Expect(cloud.ReadCount == 0);
        Expect(cloud.WriteCount == 0);
        Expect(
            cloud.TryReadSeeded(ProgressPath, out var content)
                && content == "{\"cloud\":\"unchanged\"}"
        );
    }

    private static void ExpectMirror(
        string root,
        string path,
        string expected
    )
    {
        var mirrorPath = MirrorPath(root, path);
        Expect(File.Exists(mirrorPath));
        Expect(File.ReadAllText(mirrorPath) == expected);
    }

    private static void ExpectSeeded(
        InMemoryCloudSaveStore store,
        string path,
        string expected
    )
        => Expect(
            store.TryReadSeeded(path, out var content)
                && content == expected
        );

    private static string MirrorPath(string root, string path)
    {
        LocalSaveBackupPlan.TryResolveUnderRoot(
            Path.Combine(root, LocalSaveBackupPlan.CurrentDirectoryName),
            path,
            out var mirrorPath
        );
        return mirrorPath;
    }

    private static async Task RunAsync(
        string name,
        Func<string, Task> scenario
    )
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"sts2-local-save-backup-{Guid.NewGuid():N}"
        );
        try
        {
            await scenario(root);
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
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
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
