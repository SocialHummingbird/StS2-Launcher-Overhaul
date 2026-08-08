#nullable enable

using System.Text;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using Mono.Cecil;
using Mono.Cecil.Cil;
using STS2Mobile;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace LocalGameplaySaveSafetyProbe;

internal static class Program
{
    private static readonly string[] FinalSaveMethods =
    {
        "SaveSettings",
        "SavePrefsFile",
        "SaveProgressFile",
        "SaveProfile",
    };

    private static int _passed;

    private static async Task Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0]))
        {
            throw new ArgumentException(
                "Expected the path to the upstream sts2.dll."
            );
        }

        await RunAsync(
            "representative gameplay saves use the atomic local store",
            RepresentativeGameplayWritesAreLocalAsync
        );
        await RunAsync(
            "gameplay construction cannot reach Steam Cloud",
            GameplayConstructionIsCloudIsolatedAsync
        );
        await RunAsync(
            "upstream Quit owns one ordered final-save sequence",
            () => VerifyUpstreamQuitAsync(args[0])
        );
        await RunAsync(
            "Quit restarts only after the original method",
            QuitPatchRunsAfterFinalSavesAsync
        );

        Console.WriteLine(
            $"Local gameplay save safety probe passed {_passed}/4 scenarios."
        );
    }

    private static async Task RepresentativeGameplayWritesAreLocalAsync()
    {
        var root = NewTempDirectory();
        var tracePath = Path.Combine(root, "trace.log");
        Environment.SetEnvironmentVariable("STS2_BOOTSTRAP_TRACE_FILE", tracePath);
        CloudSyncCoordinator.SetLocalBackupEnabled(false);
        var emitted = new List<string>();
        void Capture(string message) => emitted.Add(message);
        PatchHelper.LogEmitted += Capture;

        try
        {
            ISaveStore store = new AndroidLocalSaveStore(root);
            ICancellableSaveStore cancellable = (ICancellableSaveStore)store;
            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["settings.save"] = "{\"settings\":1}",
                ["profile.save"] = "{\"profile\":1}",
                ["profile1/saves/progress.save"] = "{\"progress\":1}",
                ["profile1/saves/prefs.save"] = "{\"prefs\":1}",
                ["profile1/saves/current_run.save"] = "{\"run\":1}",
            };

            store.WriteFile("settings.save", expected["settings.save"]);
            store.WriteFile(
                "profile.save",
                Encoding.UTF8.GetBytes(expected["profile.save"])
            );
            await store.WriteFileAsync(
                "profile1/saves/progress.save",
                expected["profile1/saves/progress.save"]
            );
            await store.WriteFileAsync(
                "profile1/saves/prefs.save",
                Encoding.UTF8.GetBytes(expected["profile1/saves/prefs.save"])
            );
            await cancellable.WriteFileAsync(
                "profile1/saves/current_run.save",
                expected["profile1/saves/current_run.save"],
                CancellationToken.None
            );

            foreach (var pair in expected)
            {
                var destination = Path.Combine(
                    root,
                    pair.Key.Replace('/', Path.DirectorySeparatorChar)
                );
                Expect(File.Exists(destination), $"Missing local save {pair.Key}.");
                Expect(
                    File.ReadAllText(destination) == pair.Value,
                    $"Local save content mismatch for {pair.Key}."
                );
                Expect(
                    emitted.Count(message =>
                        message.Contains("Android local", StringComparison.Ordinal)
                        && message.Contains("write:", StringComparison.Ordinal)
                        && message.Contains(pair.Key, StringComparison.Ordinal)
                    ) == 1,
                    $"Expected one committed local write trace for {pair.Key}."
                );
            }

            Expect(
                !Directory.EnumerateFiles(
                    root,
                    "*.sts2-cloud-*.tmp",
                    SearchOption.AllDirectories
                ).Any(),
                "A completed local save left an atomic staging file behind."
            );
            Expect(
                emitted.All(message =>
                    !message.Contains("SteamKit", StringComparison.OrdinalIgnoreCase)
                    && !message.Contains("Write queue", StringComparison.OrdinalIgnoreCase)
                    && !message.Contains("cloud upload", StringComparison.OrdinalIgnoreCase)
                    && !message.Contains("cloud flush", StringComparison.OrdinalIgnoreCase)
                ),
                "A representative gameplay save emitted a Steam Cloud operation."
            );
        }
        finally
        {
            PatchHelper.LogEmitted -= Capture;
            CloudSyncCoordinator.SetLocalBackupEnabled(false);
            Environment.SetEnvironmentVariable("STS2_BOOTSTRAP_TRACE_FILE", null);
            Directory.Delete(root, recursive: true);
        }
    }

    private static Task GameplayConstructionIsCloudIsolatedAsync()
    {
        using var module = ModuleDefinition.ReadModule(
            typeof(AndroidLocalSaveStore).Assembly.Location
        );
        var launcherState = RequireType(
            module,
            "STS2Mobile.Launcher.LauncherCloudSaveState"
        );
        var gameplayFactory = RequireMethod(
            launcherState,
            "CreateAndroidGameplaySaveManager"
        );
        var gameplayCalls = Calls(gameplayFactory).ToArray();
        ExpectCall(
            gameplayCalls,
            "STS2Mobile.Steam.CloudSaveStoreFactory",
            "CreateLocalStore"
        );
        ExpectCall(
            gameplayCalls,
            "MegaCrit.Sts2.Core.Saves.SaveManager",
            ".ctor"
        );
        Expect(
            gameplayCalls.All(call =>
                !call.DeclaringType.FullName.Contains(
                    "SteamKit2CloudSaveStore",
                    StringComparison.Ordinal
                )
                && call.Name != "CreateTransferCloudSaveStore"
            ),
            "The Android gameplay SaveManager can reach a Steam Cloud factory."
        );

        var localStoreType = RequireType(
            module,
            "STS2Mobile.Steam.AndroidLocalSaveStore"
        );
        foreach (var methodName in new[] { "WriteBytesFile", "WriteBytesFileAsync" })
        {
            var writeMethod = RequireMethod(localStoreType, methodName);
            ExpectCall(
                CallsIncludingStateMachine(writeMethod),
                "STS2Mobile.Steam.CancellableAtomicFile",
                "WriteAllBytesAsync"
            );
        }

        var allMethods = AllTypes(module).SelectMany(type => type.Methods).ToArray();
        var launcherCloudFactoryCalls = allMethods
            .SelectMany(method => Calls(method).Select(call => (method, call)))
            .Where(entry =>
                entry.call.DeclaringType.FullName
                    == "STS2Mobile.Steam.CloudSaveStoreFactory"
                && entry.call.Name == "CreateTransferCloudSaveStore"
            )
            .ToArray();
        var allowedLauncherSyncEntrypoints = new[]
        {
            "RunManualSyncAsync",
            "RecoverAutomaticSyncAsync",
            "ReconcileAutomaticSyncAsync",
            "BeginAutomaticGameSessionAsync",
        };
        Expect(
            launcherCloudFactoryCalls.Length
                == allowedLauncherSyncEntrypoints.Length
                && launcherCloudFactoryCalls.All(entry =>
                    entry.method.DeclaringType.FullName.Contains(
                        "CloudSyncCoordinator",
                        StringComparison.Ordinal
                    )
                    && allowedLauncherSyncEntrypoints.Any(entrypoint =>
                        entry.method.DeclaringType.FullName.Contains(
                            entrypoint,
                            StringComparison.Ordinal
                        )
                    )
                )
                && allowedLauncherSyncEntrypoints.All(entrypoint =>
                    launcherCloudFactoryCalls.Count(entry =>
                        entry.method.DeclaringType.FullName.Contains(
                            entrypoint,
                            StringComparison.Ordinal
                        )
                    ) == 1
                ),
            "The Steam-backed save-store factory is not confined to one manual or automatic launcher sync entry path."
        );

        var gameplayPatchTypes = new[]
        {
            "STS2Mobile.Patches.LauncherPatches",
            "STS2Mobile.Patches.AppLifecyclePatches",
        };
        foreach (var typeName in gameplayPatchTypes)
        {
            var type = RequireType(module, typeName);
            Expect(
                type.Methods
                    .SelectMany(Calls)
                    .All(call =>
                        !call.DeclaringType.FullName.Contains(
                            "SteamKit2CloudSaveStore",
                            StringComparison.Ordinal
                        )
                        && call.Name != "AutoSyncFileAsync"
                    ),
                $"{type.Name} still reaches in-game Steam Cloud behavior."
            );
        }

        var steamStore = RequireType(
            module,
            "STS2Mobile.Steam.SteamKit2CloudSaveStore"
        );
        Expect(
            steamStore.Methods.All(method => method.Name != "FlushActive"),
            "The obsolete global gameplay cloud flush still exists."
        );

        var disposeCalls = allMethods
            .SelectMany(method => Calls(method).Select(call => (method, call)))
            .Where(entry =>
                entry.call.DeclaringType.FullName
                    == "STS2Mobile.Steam.SteamKit2CloudSaveStore"
                && entry.call.Name == "DisposeActive"
            )
            .ToArray();
        Expect(
            disposeCalls.Length == allowedLauncherSyncEntrypoints.Length
                && disposeCalls.All(entry =>
                    entry.method.DeclaringType.FullName.Contains(
                        "CloudSyncCoordinator",
                        StringComparison.Ordinal
                    )
                    && allowedLauncherSyncEntrypoints.Any(entrypoint =>
                        entry.method.DeclaringType.FullName.Contains(
                            entrypoint,
                            StringComparison.Ordinal
                        )
                    )
                )
                && allowedLauncherSyncEntrypoints.All(entrypoint =>
                    disposeCalls.Count(entry =>
                        entry.method.DeclaringType.FullName.Contains(
                            entrypoint,
                            StringComparison.Ordinal
                        )
                    ) == 1
                ),
            "Steam Cloud disposal is not confined to one manual or automatic launcher sync cleanup path."
        );

        return Task.CompletedTask;
    }

    private static Task VerifyUpstreamQuitAsync(string upstreamAssemblyPath)
    {
        using var module = ModuleDefinition.ReadModule(upstreamAssemblyPath);
        var nGame = RequireType(module, "MegaCrit.Sts2.Core.Nodes.NGame");
        var quit = nGame.Methods.Single(method =>
            method.Name == "Quit" && method.Parameters.Count == 0
        );
        var calls = Calls(quit).ToArray();
        var indices = new List<int>();
        foreach (var finalSave in FinalSaveMethods)
        {
            var matches = calls
                .Select((call, index) => (call, index))
                .Where(entry =>
                    entry.call.DeclaringType.FullName
                        == "MegaCrit.Sts2.Core.Saves.SaveManager"
                    && entry.call.Name == finalSave
                )
                .ToArray();
            Expect(
                matches.Length == 1,
                $"NGame.Quit must call {finalSave} exactly once."
            );
            indices.Add(matches[0].index);
        }

        var treeQuit = calls
            .Select((call, index) => (call, index))
            .Single(entry =>
                entry.call.DeclaringType.FullName == "Godot.SceneTree"
                && entry.call.Name == "Quit"
            );
        indices.Add(treeQuit.index);
        Expect(
            indices.SequenceEqual(indices.OrderBy(index => index)),
            "NGame.Quit final-save calls are not in the expected order."
        );
        return Task.CompletedTask;
    }

    private static Task QuitPatchRunsAfterFinalSavesAsync()
    {
        using var module = ModuleDefinition.ReadModule(
            typeof(AppLifecyclePatches).Assembly.Location
        );
        var lifecycle = RequireType(
            module,
            "STS2Mobile.Patches.AppLifecyclePatches"
        );
        VerifyQuitIsRegisteredAsPostfix(RequireMethod(lifecycle, "Apply"));

        var quitPostfix = RequireMethod(lifecycle, "QuitPostfix");
        var quitCalls = Calls(quitPostfix).ToArray();
        Expect(
            quitCalls.Count(call =>
                call.DeclaringType.FullName == "STS2Mobile.AndroidGodotAppBridge"
                && call.Name == "RestartApp"
            ) == 1,
            "QuitPostfix must restart the launcher exactly once."
        );
        ExpectNoFinalSavesOrSteam(quitCalls, "QuitPostfix");

        foreach (var backgroundMethod in new[]
        {
            "EnterBackgroundPostfix",
            "ExitBackgroundPrefix",
        })
        {
            var backgroundCalls = Calls(
                RequireMethod(lifecycle, backgroundMethod)
            ).ToArray();
            Expect(
                backgroundCalls.All(call => call.Name != "RestartApp"),
                "Temporary backgrounding must not restart the launcher."
            );
            ExpectNoFinalSavesOrSteam(backgroundCalls, backgroundMethod);
        }
        return Task.CompletedTask;
    }

    private static void VerifyQuitIsRegisteredAsPostfix(MethodDefinition apply)
    {
        var instructions = apply.Body.Instructions;
        var quitName = instructions.Single(instruction =>
            instruction.OpCode == OpCodes.Ldstr
            && Equals(instruction.Operand, "Quit")
        );
        var quitIndex = instructions.IndexOf(quitName);
        var patchIndex = Enumerable.Range(quitIndex + 1, instructions.Count - quitIndex - 1)
            .First(index =>
                instructions[index].Operand is MethodReference call
                && call.DeclaringType.FullName == "STS2Mobile.PatchHelper"
                && call.Name == "Patch"
            );
        var registration = instructions
            .Skip(quitIndex)
            .Take(patchIndex - quitIndex + 1)
            .ToArray();

        Expect(
            registration.Length > 2 && registration[1].OpCode == OpCodes.Ldnull,
            "NGame.Quit must not have a Stage 1 prefix."
        );
        Expect(
            registration.Count(instruction =>
                instruction.OpCode == OpCodes.Ldstr
                && Equals(instruction.Operand, "QuitPostfix")
            ) == 1,
            "NGame.Quit must register exactly one QuitPostfix."
        );
        var postfixFactoryIndex = Array.FindIndex(registration, instruction =>
            instruction.Operand is MethodReference call
            && call.DeclaringType.FullName == "STS2Mobile.PatchHelper"
            && call.Name == "Method"
        );
        Expect(
            postfixFactoryIndex > 0
                && postfixFactoryIndex + 1 < registration.Length
                && registration[postfixFactoryIndex + 1].OpCode == OpCodes.Ldnull,
            "QuitPostfix must occupy the postfix argument, with no transpiler."
        );
    }

    private static void ExpectNoFinalSavesOrSteam(
        IEnumerable<MethodReference> calls,
        string methodName
    )
    {
        Expect(
            calls.All(call =>
                !(call.DeclaringType.FullName
                        == "MegaCrit.Sts2.Core.Saves.SaveManager"
                    && FinalSaveMethods.Contains(call.Name))
                && !call.DeclaringType.FullName.Contains(
                    "SteamKit2CloudSaveStore",
                    StringComparison.Ordinal
                )
            ),
            $"{methodName} duplicates final saves or reaches Steam Cloud."
        );
    }

    private static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition module)
        => module.Types.SelectMany(SelfAndNested);

    private static IEnumerable<TypeDefinition> SelfAndNested(TypeDefinition type)
    {
        yield return type;
        foreach (var nested in type.NestedTypes.SelectMany(SelfAndNested))
            yield return nested;
    }

    private static TypeDefinition RequireType(
        ModuleDefinition module,
        string fullName
    )
        => AllTypes(module).SingleOrDefault(type => type.FullName == fullName)
            ?? throw new InvalidOperationException($"Missing type {fullName}.");

    private static MethodDefinition RequireMethod(
        TypeDefinition type,
        string name
    )
        => type.Methods.SingleOrDefault(method => method.Name == name)
            ?? throw new InvalidOperationException(
                $"Missing method {type.FullName}.{name}."
            );

    private static IEnumerable<MethodReference> Calls(MethodDefinition method)
        => method.HasBody
            ? method.Body.Instructions
                .Where(instruction =>
                    instruction.OpCode == OpCodes.Call
                    || instruction.OpCode == OpCodes.Callvirt
                    || instruction.OpCode == OpCodes.Newobj
                )
                .Select(instruction => instruction.Operand)
                .OfType<MethodReference>()
            : Enumerable.Empty<MethodReference>();

    private static IEnumerable<MethodReference> CallsIncludingStateMachine(
        MethodDefinition method
    )
    {
        foreach (var call in Calls(method))
            yield return call;

        var stateMachine = method.DeclaringType.NestedTypes.SingleOrDefault(type =>
            type.Name.StartsWith($"<{method.Name}>d__", StringComparison.Ordinal)
        );
        var moveNext = stateMachine?.Methods.SingleOrDefault(candidate =>
            candidate.Name == "MoveNext"
        );
        if (moveNext is null)
            yield break;

        foreach (var call in Calls(moveNext))
            yield return call;
    }

    private static void ExpectCall(
        IEnumerable<MethodReference> calls,
        string declaringType,
        string methodName
    )
        => Expect(
            calls.Any(call =>
                call.DeclaringType.FullName == declaringType
                && call.Name == methodName
            ),
            $"Expected call to {declaringType}.{methodName}."
        );

    private static string NewTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"sts2-local-gameplay-safety-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task RunAsync(string name, Func<Task> scenario)
    {
        try
        {
            await scenario();
            _passed++;
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"FAIL: {name}: {ex.Message}", ex);
        }
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
