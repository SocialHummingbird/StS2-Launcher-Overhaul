#nullable enable

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal static class Stage5ProcessDeathProbe
{
    private const string WorkerSwitch = "--stage5-process-worker";
    private const string SetupAction = "setup";
    private const string TraceAction = "trace";
    private const string CrashAction = "crash";
    private const string RecoverAction = "recover";

    private const string BeginScenario = "automatic-begin";
    private const string UploadScenario = "automatic-upload";
    private const string PullScenario = "automatic-pull";
    private const string RestoreScenario = "recovery-restore";
    private const string UndoScenario = "recovery-undo";

    private const ulong SteamId64 = InMemoryCloudSaveStore.DefaultSteamId64;
    private const string ProfilePath = "profile.save";
    private const string ProgressPath = "profile1/saves/progress.save";
    private const string PreferencesPath = "profile1/saves/prefs.save";
    private const string CurrentRunPath = "profile1/saves/current_run.save";
    private const string A = "{\"progress\":\"a\"}";
    private const string B = "{\"progress\":\"b\"}";
    private const string PrefsA = "{\"prefs\":\"a\"}";
    private const string PrefsB = "{\"prefs\":\"b\"}";
    private const string VanillaIncompletePullPath =
        ".sts2-launcher/pull-incomplete/vanilla.json";

    private static readonly string[] Scenarios =
    {
        BeginScenario,
        UploadScenario,
        PullScenario,
        RestoreScenario,
        UndoScenario,
    };

    internal static bool IsWorkerInvocation(string[] args)
        => args.Length > 0
            && string.Equals(args[0], WorkerSwitch, StringComparison.Ordinal);

    internal static async Task RunWorkerAsync(string[] args)
    {
        if (!IsWorkerInvocation(args) || args.Length is < 4 or > 5)
        {
            throw new ArgumentException(
                $"Usage: {WorkerSwitch} <setup|trace|crash|recover> "
                    + "<scenario> <fixture-root> [target-index]"
            );
        }

        var action = args[1];
        var scenario = RequireScenario(args[2]);
        var root = Path.GetFullPath(args[3]);
        var targetIndex = action == CrashAction
            ? args.Length == 5
                ? int.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture)
                : throw new ArgumentException("Crash worker requires a target index.")
            : -1;
        if (action != CrashAction && args.Length != 4)
            throw new ArgumentException($"{action} worker does not accept a target index.");

        Directory.CreateDirectory(root);
        CloudSyncCoordinator.SetLocalBackupEnabled(false);

        switch (action)
        {
            case SetupAction:
                await SetupScenarioAsync(scenario, root).ConfigureAwait(false);
                return;
            case TraceAction:
                DeleteControlFiles(root);
                await RunScenarioOperationAsync(
                    scenario,
                    root,
                    targetIndex: null
                ).ConfigureAwait(false);
                return;
            case CrashAction:
                DeleteControlFiles(root);
                await RunScenarioOperationAsync(
                    scenario,
                    root,
                    targetIndex
                ).ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"Crash target edge {targetIndex} was not observed."
                );
            case RecoverAction:
                await RecoverScenarioAsync(scenario, root).ConfigureAwait(false);
                return;
            default:
                throw new ArgumentException($"Unknown Stage 5 worker action: {action}");
        }
    }

    internal static async Task<int> RunAllAsync()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"sts2-stage5-process-death-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(root);
        var completed = false;
        var totalEdges = 0;
        try
        {
            foreach (var scenario in Scenarios)
            {
                var scenarioRoot = Path.Combine(root, scenario);
                var templateRoot = Path.Combine(scenarioRoot, "template");
                await RequireCleanChildExitAsync(
                    SetupAction,
                    scenario,
                    templateRoot
                ).ConfigureAwait(false);

                var traceRoot = Path.Combine(scenarioRoot, "trace");
                CopyDirectoryTree(templateRoot, traceRoot);
                await RequireCleanChildExitAsync(
                    TraceAction,
                    scenario,
                    traceRoot
                ).ConfigureAwait(false);
                var canonical = ReadTrace(traceRoot);
                ExpectPairedMutationEdges(canonical, scenario);

                for (var edgeIndex = 0; edgeIndex < canonical.Count; edgeIndex++)
                {
                    var edgeRoot = Path.Combine(
                        scenarioRoot,
                        $"edge-{edgeIndex:D3}"
                    );
                    CopyDirectoryTree(templateRoot, edgeRoot);
                    await RequireHardCrashAsync(
                        scenario,
                        edgeRoot,
                        edgeIndex,
                        canonical[edgeIndex]
                    ).ConfigureAwait(false);
                    await RequireCleanChildExitAsync(
                        RecoverAction,
                        scenario,
                        edgeRoot
                    ).ConfigureAwait(false);
                    ValidateFinalOnDiskState(scenario, edgeRoot);
                    DeleteOwnedDirectory(edgeRoot);
                }

                totalEdges += canonical.Count;
                Console.WriteLine(
                    $"  hard process restart edges: {scenario}={canonical.Count}"
                );
                DeleteOwnedDirectory(scenarioRoot);
            }

            completed = true;
            Console.WriteLine(
                $"Stage 5 filesystem process-death probe passed {totalEdges}/{totalEdges} "
                    + "before/after persisted mutation edges."
            );
            return totalEdges;
        }
        finally
        {
            if (completed)
                DeleteOwnedDirectory(root);
            else
                Console.Error.WriteLine(
                    $"Stage 5 process-death evidence retained after failure: {root}"
                );
        }
    }

    private static async Task SetupScenarioAsync(string scenario, string root)
    {
        EnsureFreshFixtureRoot(root);
        var local = new ObservedAndroidLocalSaveStore(LocalRoot(root));
        var cloud = new InMemoryCloudSaveStore("stage5-cloud", SteamId64);

        if (IsAutomaticScenario(scenario))
        {
            SeedLocal(local, ProfilePath, "{\"profile\":1}");
            SeedLocal(local, ProgressPath, A);
            SeedLocal(local, PreferencesPath, PrefsA);
            cloud.Seed(ProfilePath, "{\"profile\":1}");
            cloud.Seed(ProgressPath, A);
            cloud.Seed(PreferencesPath, PrefsA);
            SeedRemoteContext(cloud);

            var established = await ReconcileAsync(
                local,
                cloud,
                AutomaticSyncSourceChoice.Local
            ).ConfigureAwait(false);
            ExpectOutcome(
                established,
                AutomaticSyncOutcome.Synchronized,
                "establish automatic baseline"
            );

            if (scenario != BeginScenario)
            {
                var prepared = await BeginAsync(local, cloud)
                    .ConfigureAwait(false);
                ExpectOutcome(
                    prepared,
                    AutomaticSyncOutcome.GameSessionPrepared,
                    "prepare automatic session"
                );
            }

            if (scenario == UploadScenario)
            {
                SeedLocal(local, ProgressPath, B);
                SeedLocal(local, PreferencesPath, PrefsB);
            }
            else if (scenario == PullScenario)
            {
                cloud.Seed(ProgressPath, B);
                ((ISaveStore)cloud).DeleteFile(PreferencesPath);
            }
        }
        else
        {
            await SetupRecoveryScenarioAsync(scenario, root, local)
                .ConfigureAwait(false);
        }

        PersistedFakeSteamStore.Save(CloudStatePath(root), cloud);
    }

    private static async Task SetupRecoveryScenarioAsync(
        string scenario,
        string root,
        ObservedAndroidLocalSaveStore local
    )
    {
        SeedLocal(local, ProfilePath, "{\"profile\":1}");
        SeedLocalBytes(local, ProgressPath, RecoveryAdvancedProgress());
        SeedLocalBytes(local, PreferencesPath, RecoveryAdvancedPreferences());
        var source = await CloudSyncCoordinator.CaptureRetainedLocalSnapshotAsync(
            local,
            Context(),
            "stage5-process-source",
            CancellationToken.None
        ).ConfigureAwait(false);
        PersistedFakeSteamStore.AtomicWriteText(
            SourcePathRecord(root),
            source.Path
        );

        SeedLocalBytes(local, ProgressPath, RecoveryOriginalProgress());
        SeedLocalBytes(local, PreferencesPath, RecoveryOriginalPreferences());
        SeedLocalBytes(local, CurrentRunPath, RecoveryOriginalRun());

        if (scenario == UndoScenario)
        {
            var restored = await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                local,
                source.Path,
                SaveNamespace.Vanilla,
                SteamGameBranch.Public,
                "",
                SteamId64,
                CancellationToken.None
            ).ConfigureAwait(false);
            Expect(
                restored.Status.CanUndo && restored.Status.SyncHeld,
                "Recovery Undo fixture was not left in validation-required state."
            );
            RequireRestoredBytes(root);
        }
    }

    private static async Task RunScenarioOperationAsync(
        string scenario,
        string root,
        int? targetIndex
    )
    {
        var cloud = PersistedFakeSteamStore.Load(
            CloudStatePath(root),
            "stage5-cloud"
        );
        var controller = new ProcessDeathMutationController(
            root,
            cloud,
            targetIndex
        );
        var local = new ObservedAndroidLocalSaveStore(
            LocalRoot(root),
            controller.Observe
        );
        cloud.MutationObserved = controller.Observe;

        switch (scenario)
        {
            case BeginScenario:
            {
                var result = await BeginAsync(local, cloud).ConfigureAwait(false);
                ExpectOutcome(
                    result,
                    AutomaticSyncOutcome.GameSessionPrepared,
                    "trace before-game preparation"
                );
                break;
            }
            case UploadScenario:
            case PullScenario:
            {
                var result = await RecoverAsync(local, cloud).ConfigureAwait(false);
                ExpectOutcome(
                    result,
                    AutomaticSyncOutcome.Synchronized,
                    $"trace {scenario}"
                );
                break;
            }
            case RestoreScenario:
            {
                var result = await CloudSyncCoordinator.RestoreSaveSnapshotAsync(
                    local,
                    ReadSourcePath(root),
                    SaveNamespace.Vanilla,
                    SteamGameBranch.Public,
                    "",
                    SteamId64,
                    CancellationToken.None
                ).ConfigureAwait(false);
                Expect(
                    result.Status.CanUndo && result.Status.SyncHeld,
                    "Restore trace did not reach validation-required state."
                );
                RequireRestoredBytes(root);
                break;
            }
            case UndoScenario:
            {
                var result = await CloudSyncCoordinator.UndoSaveRecoveryAsync(
                    local,
                    CancellationToken.None
                ).ConfigureAwait(false);
                Expect(
                    !result.Status.CanUndo && !result.Status.SyncHeld,
                    "Undo trace did not reach its terminal state."
                );
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown scenario: {scenario}");
        }

        cloud.MutationObserved = null;
        PersistedFakeSteamStore.Save(CloudStatePath(root), cloud);
    }

    private static async Task RecoverScenarioAsync(string scenario, string root)
    {
        var local = new ObservedAndroidLocalSaveStore(LocalRoot(root));
        var cloud = PersistedFakeSteamStore.Load(
            CloudStatePath(root),
            "stage5-cloud"
        );

        switch (scenario)
        {
            case BeginScenario:
                await RecoverUntilSynchronizedAsync(local, cloud)
                    .ConfigureAwait(false);
                var prepared = await BeginAsync(local, cloud).ConfigureAwait(false);
                ExpectOutcome(
                    prepared,
                    AutomaticSyncOutcome.GameSessionPrepared,
                    "restart before-game preparation"
                );
                await RecoverUntilSynchronizedAsync(local, cloud)
                    .ConfigureAwait(false);
                break;
            case UploadScenario:
            case PullScenario:
                await RecoverUntilSynchronizedAsync(local, cloud)
                    .ConfigureAwait(false);
                break;
            case RestoreScenario:
            {
                var status = CloudSyncCoordinator.InspectSaveRecoveryStatus(local);
                if (!status.CanUndo)
                {
                    var restored = await CloudSyncCoordinator
                        .RestoreSaveSnapshotAsync(
                            local,
                            ReadSourcePath(root),
                            SaveNamespace.Vanilla,
                            SteamGameBranch.Public,
                            "",
                            SteamId64,
                            CancellationToken.None
                        ).ConfigureAwait(false);
                    Expect(restored.Status.CanUndo, "Restore retry did not become undoable.");
                }

                var undone = await CloudSyncCoordinator.UndoSaveRecoveryAsync(
                    local,
                    CancellationToken.None
                ).ConfigureAwait(false);
                Expect(!undone.Status.CanUndo, "Restarted Restore could not be undone.");
                break;
            }
            case UndoScenario:
            {
                var undone = await CloudSyncCoordinator.UndoSaveRecoveryAsync(
                    local,
                    CancellationToken.None
                ).ConfigureAwait(false);
                Expect(!undone.Status.CanUndo, "Interrupted Undo did not converge.");
                break;
            }
            default:
                throw new InvalidOperationException($"Unknown scenario: {scenario}");
        }

        PersistedFakeSteamStore.Save(CloudStatePath(root), cloud);
        ValidateFinalWorkerState(scenario, local, cloud);
    }

    private static async Task RecoverUntilSynchronizedAsync(
        ISaveStore local,
        ICloudSaveStore cloud
    )
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var result = await RecoverAsync(local, cloud).ConfigureAwait(false);
            if (result.Outcome == AutomaticSyncOutcome.Synchronized)
                return;
            Expect(
                result.Outcome == AutomaticSyncOutcome.PendingRecoveryRequired,
                $"Hard restart recovery stopped as {result.Outcome}: {result.Message}"
            );
        }

        throw new InvalidOperationException(
            "Hard restart recovery did not settle after four unchanged retries."
        );
    }

    private static void ValidateFinalWorkerState(
        string scenario,
        ISaveStore local,
        InMemoryCloudSaveStore cloud
    )
    {
        if (IsAutomaticScenario(scenario))
        {
            Expect(
                !CloudSyncCoordinator.HasPendingAutomaticSync(local),
                $"{scenario} retained pending-sync.json after restart recovery."
            );
            Expect(
                !local.FileExists(VanillaIncompletePullPath),
                $"{scenario} retained the incomplete Pull marker."
            );
            var context = Context();
            Expect(
                cloud.TryReadSeeded(context.MarkerPath, out var marker)
                    && string.Equals(
                        marker,
                        context.SerializeMarker(),
                        StringComparison.Ordinal
                    ),
                $"{scenario} did not converge to the exact remote context marker."
            );
            return;
        }

        var status = CloudSyncCoordinator.InspectSaveRecoveryStatus(local);
        Expect(!status.CanUndo && !status.SyncHeld, $"{scenario} did not finish Undo.");
    }

    private static void ValidateFinalOnDiskState(string scenario, string root)
    {
        var cloud = PersistedFakeSteamStore.Load(
            CloudStatePath(root),
            "stage5-validation-cloud"
        );
        if (scenario == BeginScenario)
        {
            RequireLocalHash(root, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
            RequireLocalHash(root, ProgressPath, Encoding.UTF8.GetBytes(A));
            RequireLocalHash(root, PreferencesPath, Encoding.UTF8.GetBytes(PrefsA));
            RequireCloudHash(cloud, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
            RequireCloudHash(cloud, ProgressPath, Encoding.UTF8.GetBytes(A));
            RequireCloudHash(cloud, PreferencesPath, Encoding.UTF8.GetBytes(PrefsA));
        }
        else if (scenario == UploadScenario)
        {
            RequireLocalHash(root, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
            RequireLocalHash(root, ProgressPath, Encoding.UTF8.GetBytes(B));
            RequireLocalHash(root, PreferencesPath, Encoding.UTF8.GetBytes(PrefsB));
            RequireCloudHash(cloud, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
            RequireCloudHash(cloud, ProgressPath, Encoding.UTF8.GetBytes(B));
            RequireCloudHash(cloud, PreferencesPath, Encoding.UTF8.GetBytes(PrefsB));
        }
        else if (scenario == PullScenario)
        {
            RequireLocalHash(root, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
            RequireLocalHash(root, ProgressPath, Encoding.UTF8.GetBytes(B));
            RequireCloudHash(cloud, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
            RequireCloudHash(cloud, ProgressPath, Encoding.UTF8.GetBytes(B));
            RequireLocalMissing(root, PreferencesPath);
            Expect(
                !cloud.Contains(PreferencesPath),
                "Pull restart validation found a remote preferences tombstone mismatch."
            );
        }
        else
        {
            RequireLocalHash(root, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
            RequireLocalHash(root, ProgressPath, RecoveryOriginalProgress());
            RequireLocalHash(root, PreferencesPath, RecoveryOriginalPreferences());
            RequireLocalHash(root, CurrentRunPath, RecoveryOriginalRun());
            var local = new AndroidLocalSaveStore(LocalRoot(root));
            var status = CloudSyncCoordinator.InspectSaveRecoveryStatus(local);
            Expect(
                !status.CanUndo && !status.SyncHeld,
                $"{scenario} on-disk journal did not reach undone."
            );
        }

        RequireLocalMissing(root, CloudSyncCoordinator.AutomaticSyncPendingPath);
        RequireLocalMissing(root, VanillaIncompletePullPath);
    }

    private static void RequireRestoredBytes(string root)
    {
        RequireLocalHash(root, ProfilePath, Encoding.UTF8.GetBytes("{\"profile\":1}"));
        RequireLocalHash(root, ProgressPath, RecoveryAdvancedProgress());
        RequireLocalHash(root, PreferencesPath, RecoveryAdvancedPreferences());
        RequireLocalMissing(root, CurrentRunPath);
    }

    private static void RequireLocalHash(
        string root,
        string relativePath,
        byte[] expected
    )
    {
        var path = LocalFile(root, relativePath);
        Expect(File.Exists(path), $"Expected on-disk local file is missing: {relativePath}");
        var actualHash = AutomaticSyncHash.Compute(File.ReadAllBytes(path));
        var expectedHash = AutomaticSyncHash.Compute(expected);
        Expect(
            string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase),
            $"On-disk local hash mismatch for {relativePath}."
        );
    }

    private static void RequireCloudHash(
        InMemoryCloudSaveStore cloud,
        string path,
        byte[] expected
    )
    {
        Expect(
            cloud.TryReadSeededBytes(path, out var actual),
            $"Expected persisted fake Steam file is missing: {path}"
        );
        Expect(
            string.Equals(
                AutomaticSyncHash.Compute(actual),
                AutomaticSyncHash.Compute(expected),
                StringComparison.OrdinalIgnoreCase
            ),
            $"Persisted fake Steam hash mismatch for {path}."
        );
    }

    private static void RequireLocalMissing(string root, string relativePath)
        => Expect(
            !File.Exists(LocalFile(root, relativePath)),
            $"Unexpected on-disk local file remains: {relativePath}"
        );

    private static async Task RequireHardCrashAsync(
        string scenario,
        string root,
        int edgeIndex,
        MutationTrace expected
    )
    {
        var result = await RunChildAsync(
            CrashAction,
            scenario,
            root,
            edgeIndex
        ).ConfigureAwait(false);
        Expect(
            result.ExitCode != 0,
            $"{scenario} edge {edgeIndex} returned normally instead of terminating."
        );
        Expect(
            File.Exists(HitPath(root)),
            $"{scenario} edge {edgeIndex} exited without its durable kill marker. "
                + result.ErrorSummary
        );
        var trace = ReadTrace(root);
        Expect(
            trace.Count == edgeIndex + 1,
            $"{scenario} edge {edgeIndex} reached {trace.Count} mutations before death."
        );
        Expect(
            SameMutationShape(trace[^1], expected),
            $"{scenario} edge {edgeIndex} was nondeterministic. Expected "
                + $"{MutationShape(expected)}, saw {MutationShape(trace[^1])}."
        );
    }

    private static async Task RequireCleanChildExitAsync(
        string action,
        string scenario,
        string root
    )
    {
        var result = await RunChildAsync(action, scenario, root, null)
            .ConfigureAwait(false);
        Expect(
            result.ExitCode == 0,
            $"Stage 5 child failed: action={action}, scenario={scenario}, "
                + $"exit={result.ExitCode}. {result.ErrorSummary}"
        );
    }

    private static async Task<ChildResult> RunChildAsync(
        string action,
        string scenario,
        string root,
        int? targetIndex
    )
    {
        var start = CreateWorkerStartInfo();
        start.ArgumentList.Add(WorkerSwitch);
        start.ArgumentList.Add(action);
        start.ArgumentList.Add(scenario);
        start.ArgumentList.Add(root);
        if (targetIndex.HasValue)
        {
            start.ArgumentList.Add(
                targetIndex.Value.ToString(
                    System.Globalization.CultureInfo.InvariantCulture
                )
            );
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start Stage 5 child process.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync();
        var completed = await Task.WhenAny(
            waitTask,
            Task.Delay(TimeSpan.FromSeconds(30))
        ).ConfigureAwait(false);
        if (!ReferenceEquals(completed, waitTask))
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);
            throw new TimeoutException(
                $"Stage 5 child timed out: {action} {scenario}"
            );
        }

        await waitTask.ConfigureAwait(false);
        return new ChildResult(
            process.ExitCode,
            await stdoutTask.ConfigureAwait(false),
            await stderrTask.ConfigureAwait(false)
        );
    }

    private static ProcessStartInfo CreateWorkerStartInfo()
    {
        var processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Current process path is unavailable.");
        var start = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        if (string.Equals(
                Path.GetFileNameWithoutExtension(processPath),
                "dotnet",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        }
        return start;
    }

    private static List<MutationTrace> ReadTrace(string root)
    {
        var path = TracePath(root);
        Expect(File.Exists(path), $"Mutation trace is missing: {path}");
        return File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<MutationTrace>(line)
                ?? throw new InvalidDataException("Mutation trace entry is empty."))
            .ToList();
    }

    private static void ExpectPairedMutationEdges(
        IReadOnlyList<MutationTrace> mutations,
        string scenario
    )
    {
        Expect(mutations.Count > 0, $"{scenario} persisted no state.");
        Expect(
            mutations.Count % 2 == 0,
            $"{scenario} exposed an unpaired persistence edge."
        );
        for (var index = 0; index < mutations.Count; index += 2)
        {
            var before = mutations[index];
            var after = mutations[index + 1];
            Expect(
                string.Equals(before.Edge, nameof(StoreMutationEdge.Before), StringComparison.Ordinal)
                    && string.Equals(after.Edge, nameof(StoreMutationEdge.After), StringComparison.Ordinal)
                    && string.Equals(before.Store, after.Store, StringComparison.Ordinal)
                    && string.Equals(before.Kind, after.Kind, StringComparison.Ordinal)
                    && string.Equals(before.Path, after.Path, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        before.DestinationPath,
                        after.DestinationPath,
                        StringComparison.OrdinalIgnoreCase
                    ),
                $"{scenario} mutation edges are not paired at {index}."
            );
        }
    }

    private static bool SameMutationShape(MutationTrace left, MutationTrace right)
        => string.Equals(
            MutationShape(left),
            MutationShape(right),
            StringComparison.OrdinalIgnoreCase
        );

    private static string MutationShape(MutationTrace mutation)
        => $"{mutation.Store}:{mutation.Kind}:{mutation.Edge}:"
            + $"{StablePath(mutation.Path)}->{StablePath(mutation.DestinationPath)}";

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
            "{volatile-id}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
        );
    }

    private static void CopyDirectoryTree(string source, string destination)
    {
        Expect(Directory.Exists(source), $"Fixture template is missing: {source}");
        Expect(!Directory.Exists(destination), $"Fixture destination exists: {destination}");
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(
            source,
            "*",
            SearchOption.AllDirectories
        ))
        {
            Directory.CreateDirectory(Path.Combine(
                destination,
                Path.GetRelativePath(source, directory)
            ));
        }
        foreach (var file in Directory.GetFiles(
            source,
            "*",
            SearchOption.AllDirectories
        ))
        {
            var target = Path.Combine(
                destination,
                Path.GetRelativePath(source, file)
            );
            Directory.CreateDirectory(
                Path.GetDirectoryName(target)
                    ?? throw new InvalidOperationException("Fixture file has no parent.")
            );
            File.Copy(file, target, overwrite: false);
        }
    }

    private static void EnsureFreshFixtureRoot(string root)
    {
        if (Directory.EnumerateFileSystemEntries(root).Any())
            throw new InvalidOperationException($"Fixture root is not empty: {root}");
        Directory.CreateDirectory(LocalRoot(root));
    }

    private static void DeleteOwnedDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var temp = Path.GetFullPath(Path.GetTempPath());
        var name = new DirectoryInfo(fullPath).Name;
        var owned = fullPath.StartsWith(temp, StringComparison.OrdinalIgnoreCase)
            && (name.StartsWith("sts2-stage5-process-death-", StringComparison.Ordinal)
                || fullPath.Contains(
                    $"{Path.DirectorySeparatorChar}sts2-stage5-process-death-",
                    StringComparison.Ordinal
                ));
        if (!owned)
            throw new InvalidOperationException($"Refusing to delete unowned path: {fullPath}");
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
    }

    private static void DeleteControlFiles(string root)
    {
        foreach (var path in new[] { TracePath(root), HitPath(root) })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static string RequireScenario(string value)
        => Scenarios.Contains(value, StringComparer.Ordinal)
            ? value
            : throw new ArgumentException($"Unknown Stage 5 scenario: {value}");

    private static bool IsAutomaticScenario(string scenario)
        => scenario is BeginScenario or UploadScenario or PullScenario;

    private static SaveContext Context()
        => SaveContext.Create(
            SteamId64,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            ""
        );

    private static void SeedRemoteContext(InMemoryCloudSaveStore cloud)
    {
        var context = Context();
        cloud.Seed(context.MarkerPath, context.SerializeMarker());
    }

    private static Task<AutomaticSyncResult> ReconcileAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        AutomaticSyncSourceChoice? sourceChoice
    )
        => CloudSyncCoordinator.ReconcileAutomaticSyncAsync(
            local,
            cloud,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            sourceChoice,
            Progress(),
            CancellationToken.None
        );

    private static Task<AutomaticSyncResult> BeginAsync(
        ISaveStore local,
        ICloudSaveStore cloud
    )
        => CloudSyncCoordinator.BeginAutomaticGameSessionAsync(
            local,
            cloud,
            SaveNamespace.Vanilla,
            SteamGameBranch.Public,
            "",
            Progress(),
            CancellationToken.None
        );

    private static Task<AutomaticSyncResult> RecoverAsync(
        ISaveStore local,
        ICloudSaveStore cloud
    )
        => CloudSyncCoordinator.RecoverAutomaticSyncAsync(
            local,
            cloud,
            Progress(),
            CancellationToken.None
        );

    private static CloudOperationProgressTracker Progress()
        => new(CloudOperationKind.Push);

    private static void ExpectOutcome(
        AutomaticSyncResult result,
        AutomaticSyncOutcome expected,
        string operation
    )
        => Expect(
            result.Outcome == expected,
            $"Could not {operation}: {result.Outcome}: {result.Message}"
        );

    private static void SeedLocal(
        ISaveStore local,
        string path,
        string content
    )
        => local.WriteFile(path, content);

    private static void SeedLocalBytes(
        ISaveStore local,
        string path,
        byte[] content
    )
        => local.WriteFile(path, content);

    private static byte[] RecoveryAdvancedProgress()
        => SpecialBytes("{\"ironclad\":10,\"regent\":8}\r\n\0");

    private static byte[] RecoveryAdvancedPreferences()
        => SpecialBytes("{\"recovery\":\"advanced\"}\r\n\0");

    private static byte[] RecoveryOriginalProgress()
        => SpecialBytes("{\"ironclad\":7}\r\n\0");

    private static byte[] RecoveryOriginalPreferences()
        => SpecialBytes("{\"recovery\":\"original\"}\r\n\0");

    private static byte[] RecoveryOriginalRun()
        => SpecialBytes("{\"floor\":12}\r\n\0");

    private static byte[] SpecialBytes(string content)
        => Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(content))
            .ToArray();

    private static string LocalRoot(string root) => Path.Combine(root, "local");
    private static string CloudStatePath(string root)
        => Path.Combine(root, "cloud-state.json");
    private static string TracePath(string root)
        => Path.Combine(root, "mutation-trace.ndjson");
    private static string HitPath(string root)
        => Path.Combine(root, "hard-kill-hit.json");
    private static string SourcePathRecord(string root)
        => Path.Combine(root, "recovery-source.txt");

    private static string ReadSourcePath(string root)
        => File.ReadAllText(SourcePathRecord(root)).Trim();

    private static string LocalFile(string root, string relativePath)
        => Path.GetFullPath(Path.Combine(
            LocalRoot(root),
            CloudSavePath.Relative(relativePath).Replace(
                '/',
                Path.DirectorySeparatorChar
            )
        ));

    private static void Expect(
        bool condition,
        string message
    )
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record MutationTrace(
        string Store,
        string Kind,
        string Edge,
        string Path,
        string DestinationPath
    );

    private sealed record ChildResult(
        int ExitCode,
        string StandardOutput,
        string StandardError
    )
    {
        internal string ErrorSummary
        {
            get
            {
                var combined = $"{StandardError}\n{StandardOutput}".Trim();
                return combined.Length <= 1200
                    ? combined
                    : combined[^1200..];
            }
        }
    }

    private sealed class ProcessDeathMutationController
    {
        private readonly InMemoryCloudSaveStore _cloud;
        private readonly string _root;
        private readonly int? _targetIndex;
        private int _index;

        internal ProcessDeathMutationController(
            string root,
            InMemoryCloudSaveStore cloud,
            int? targetIndex
        )
        {
            _root = root;
            _cloud = cloud;
            _targetIndex = targetIndex;
        }

        internal void Observe(StoreMutation mutation)
        {
            var trace = new MutationTrace(
                mutation.Store.EndsWith("cloud", StringComparison.OrdinalIgnoreCase)
                    ? "cloud"
                    : "local",
                mutation.Kind.ToString(),
                mutation.Edge.ToString(),
                mutation.Path,
                mutation.DestinationPath
            );
            AppendTrace(TracePath(_root), JsonSerializer.Serialize(trace));
            var index = _index++;
            if (_targetIndex != index)
                return;

            PersistedFakeSteamStore.Save(CloudStatePath(_root), _cloud);
            PersistedFakeSteamStore.AtomicWriteText(
                HitPath(_root),
                JsonSerializer.Serialize(new
                {
                    Index = index,
                    Mutation = trace,
                })
            );
            Process.GetCurrentProcess().Kill();
            Thread.Sleep(Timeout.Infinite);
        }

        private static void AppendTrace(string path, string json)
        {
            var parent = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Trace path has no parent.");
            Directory.CreateDirectory(parent);
            using var stream = new FileStream(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.WriteThrough
            );
            var bytes = Encoding.UTF8.GetBytes(json + "\n");
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(flushToDisk: true);
        }
    }
}
