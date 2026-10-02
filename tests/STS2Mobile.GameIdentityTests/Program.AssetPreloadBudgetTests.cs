using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
using Mono.Cecil;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    // Used by the optional Android mode of the existing resource probe. The
    // real Mono runtime must collect while its native host waits for a worker.
    [UnmanagedCallersOnly]
    private static int AndroidMonoNativeProbe() => 1;

    [UnmanagedCallersOnly]
    private static int AndroidMonoCollectProbe()
    {
        GC.Collect();
        return 1;
    }

    private static void WriteAndroidMonoFixture(string directory)
    {
        Directory.CreateDirectory(directory);
        File.Copy(typeof(Program).Assembly.Location,
            Path.Combine(directory, "STS2Mobile.GameIdentityTests.dll"), overwrite: true);
        File.WriteAllText(Path.Combine(directory, "native-transition.cpp"), """
            #include <dlfcn.h>
            #include <cstdio>
            #include <cstdlib>
            #include <string>
            #include <thread>
            int main(int argc, char **argv) {
                if (argc < 2) return 1;
                const char *root = argv[1];
                void *runtime = dlopen((std::string(root) + "/libmonosgen-2.0.so").c_str(), RTLD_NOW | RTLD_GLOBAL);
                if (!runtime) { std::fprintf(stderr, "%s\n", dlerror()); return 2; }
                auto log = (void (*)(void (*)(const char *, const char *, const char *, int, void *), void *))dlsym(runtime, "mono_trace_set_log_handler");
                if (!log) return 3;
                log(+[](const char *, const char *, const char *message, int fatal, void *) {
                    std::fprintf(stderr, "%s\n", message);
                    if (fatal) std::exit(70);
                }, nullptr);
                auto initialize = (int (*)(const char *, const char *, int, const char **, const char **, void **, unsigned *))dlsym(runtime, "coreclr_initialize");
                auto create = (int (*)(void *, unsigned, const char *, const char *, const char *, void **))dlsym(runtime, "coreclr_create_delegate");
                auto native = (void *(*)(void **))dlsym(runtime, "mono_threads_enter_gc_safe_region_unbalanced");
                if (!initialize || !create || !native) return 3;
                std::string corelib = std::string(root) + "/System.Private.CoreLib.dll";
                const char *keys[] = {"APP_PATHS", "NATIVE_DLL_SEARCH_DIRECTORIES", "TRUSTED_PLATFORM_ASSEMBLIES"};
                const char *values[] = {root, root, corelib.c_str()};
                void *handle = nullptr;
                unsigned domain = 0;
                if (initialize(nullptr, nullptr, 3, keys, values, &handle, &domain)) return 4;
                int (*probe)() = nullptr;
                int (*collect)() = nullptr;
                const char *assembly = "STS2Mobile.GameIdentityTests";
                const char *type = "STS2Mobile.GameIdentityTests.Program";
                if (create(handle, domain, assembly, type, "AndroidMonoNativeProbe", (void **)&probe) || !probe) return 5;
                void *stack_marker = nullptr;
                if (argc > 2) native(&stack_marker);
                // Later delegate creation and reverse callbacks must preserve
                // the native host's GC state, including after collection.
                if (probe() != 1) return 6;
                if (create(handle, domain, assembly, type, "AndroidMonoCollectProbe", (void **)&collect) || !collect) return 7;
                std::puts("ANDROID_MONO_GC_BEGIN");
                std::fflush(stdout);
                int collections = 0;
                std::thread worker([&] { for (int i = 0; i < 64; ++i) collections += collect(); });
                worker.join();
                if (collections != 64 || probe() != 1) return 8;
                std::puts("ANDROID_MONO_GC_PASS collections=64 callbacks=preserved");
                return 0;
            }
            """);
    }

    private static void AssetPreloadBudgetPreservesQueuedWork()
    {
        var pending = new Queue<int>();
        for (var i = 0; i < 100; i++) pending.Enqueue(i);
        var loaded = new List<int>();
        var frames = 0;
        while (pending.Count > 0)
        {
            var before = loaded.Count;
            var budget = new AssetPreloadFrameBudget(4, 4, () => 0);
            while (pending.Count > 0 && budget.TryBeginItem()) loaded.Add(pending.Dequeue());
            True(loaded.Count - before <= 4, "A frame cannot drain the entire resource backlog.");
            frames++;
        }
        Equal(25, frames, "All assets must eventually finish across bounded frames.");
        for (var i = 0; i < 100; i++) Equal(i, loaded[i], "FIFO loading order must survive yielding.");
    }

    private static void AssetPreloadBudgetYieldsAfterExpensiveItem()
    {
        double elapsed = 0;
        var budget = new AssetPreloadFrameBudget(8, 4, () => elapsed);
        True(budget.TryBeginItem(), "Every frame must make progress.");
        elapsed = 5;
        True(!budget.TryBeginItem(), "An expensive resource must yield before loading another.");
        var nextFrame = new AssetPreloadFrameBudget(8, 4, () => elapsed);
        True(nextFrame.TryBeginItem(), "The next frame gets a fresh budget.");
        elapsed += 4;
        True(!nextFrame.TryBeginItem(), "The time bound must be relative to each frame.");
    }

    // The production change caught here is excluding public members after the
    // Android publicizer has rewritten the actual game assembly.
    private static void AssetPreloadPatchTargetsSurvivePublicizer()
    {
        foreach (var publicize in new[] { false, true })
        {
            using var fixture = new PreloadAssemblyFixture(publicize);
            fixture.CheckTargets();
        }
    }

    private static void AssetPreloadPatchTargetsRejectChangedShape()
    {
        var changes = new (string Target, Action<TypeDefinition, ModuleDefinition> Mutate)[]
        {
            ("_loading", (type, _) => type.Fields.Single(field => field.Name == "_loading").Name = "removed_loading"),
            ("_totalLoaded", (type, module) => type.Fields.Single(field => field.Name == "_totalLoaded").FieldType = module.TypeSystem.Int64),
            ("CheckLoadingStatus", (type, _) => type.Methods.Single(method => method.Name == "CheckLoadingStatus").Name = "RemovedCheckLoadingStatus"),
            ("ProcessLoadingQueue", (type, module) => type.Methods.Single(method => method.Name == "ProcessLoadingQueue").Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32))),
            ("FinalizeLoading", (type, module) => type.Methods.Single(method => method.Name == "FinalizeLoading").ReturnType = module.TypeSystem.Int32),
        };
        foreach (var publicize in new[] { false, true })
        foreach (var change in changes)
        {
            using var fixture = new PreloadAssemblyFixture(publicize, change.Mutate);
            try
            {
                fixture.CheckTargets();
            }
            catch (InvalidOperationException ex)
            {
                Contains(ex.Message, "Asset preload budget unsupported", "Incompatible targets must retain the production rejection.");
                Contains(ex.Message, change.Target, "Reject the actual changed target, not an unrelated visibility mismatch.");
                continue;
            }
            throw new InvalidOperationException("An incompatible preload target was accepted: " + change.Target);
        }
    }

    // Fixture/loading helpers stay with these tests. GodotSharp and the selected
    // game copy remain in a collectible context, outside the managed test host.
    private sealed class PreloadAssemblyFixture : IDisposable
    {
        private const string SessionTypeName = "MegaCrit.Sts2.Core.Assets.AssetLoadingSession";
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "sts2-preload-targets-" + Guid.NewGuid().ToString("N"));
        private readonly string _source;
        private readonly string _sourceHash;
        private readonly string _assemblyPath;
        private readonly string[] _probeDirectories;
        private readonly bool _publicized;

        internal PreloadAssemblyFixture(bool publicize, Action<TypeDefinition, ModuleDefinition>? mutate = null)
        {
            var referenceDirectory = FindPreloadReferenceDirectory();
            var steamDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam", "steamapps", "common", "Slay the Spire 2", "data_sts2_windows_x86_64");
            _probeDirectories = new[] { AppContext.BaseDirectory, referenceDirectory, steamDirectory }
                .Where(Directory.Exists).ToArray();
            _source = Path.Combine(referenceDirectory, "sts2.dll");
            _sourceHash = GameIdentityFileHasher.Instance.Sha256(_source);
            _assemblyPath = Path.Combine(_directory, "sts2.dll");
            _publicized = publicize;
            Directory.CreateDirectory(_directory);
            try
            {
                File.Copy(_source, _assemblyPath);
                if (publicize)
                {
                    var result = AndroidAssemblyPublicizer.Publicize(_assemblyPath, _probeDirectories);
                    True(result.Changed && result.FieldCount > 0 && result.MethodCount > 0,
                        "The fixture must run the actual Android publicizer.");
                }
                if (mutate != null)
                {
                    using var resolver = new DefaultAssemblyResolver();
                    foreach (var directory in _probeDirectories) resolver.AddSearchDirectory(directory);
                    using var input = new MemoryStream(File.ReadAllBytes(_assemblyPath));
                    using var module = ModuleDefinition.ReadModule(input, new ReaderParameters
                    {
                        AssemblyResolver = resolver, InMemory = true, ReadingMode = ReadingMode.Immediate,
                    });
                    mutate(module.GetType(SessionTypeName), module);
                    module.Write(_assemblyPath);
                }
            }
            catch
            {
                Directory.Delete(_directory, recursive: true);
                throw;
            }
        }

        internal void CheckTargets()
        {
            var context = new PreloadAssemblyContext(_assemblyPath, _probeDirectories);
            try
            {
                var game = context.LoadFromAssemblyName(new AssemblyName("sts2"));
                var launcher = context.LoadFromAssemblyName(new AssemblyName("STS2Mobile"));
                var patch = launcher.GetType("STS2Mobile.Patches.AndroidAssetPreloadPatches", throwOnError: true)!;
                var session = game.GetType(SessionTypeName, throwOnError: true)!;
                var resource = context.LoadFromAssemblyName(new AssemblyName("GodotSharp"))
                    .GetType("Godot.Resource", throwOnError: true)!;
                var fields = new (string Name, Type Expected)[]
                {
                    ("_loading", typeof(Queue<string>)), ("_toLoad", typeof(Queue<string>)),
                    ("_finalizing", typeof(Queue<string>)),
                    ("_cache", typeof(ConcurrentDictionary<,>).MakeGenericType(typeof(string), resource)),
                    ("_totalLoaded", typeof(int)),
                    ("_assetCache", game.GetType("MegaCrit.Sts2.Core.Assets.AssetCache", throwOnError: true)!),
                };
                foreach (var field in fields)
                {
                    Invoke(patch, "RequireField", session, field.Name, field.Expected);
                    var actual = session.GetField(field.Name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
                    Equal(_publicized, actual.IsPublic, "Exercise both original and Android member visibility.");
                }
                foreach (var name in new[] { "ProcessLoadingQueue", "FinalizeLoading", "CheckLoadingStatus" })
                {
                    var method = (MethodInfo)Invoke(patch, "RequireMethod", session, name)!;
                    Equal(name, method.Name, "Resolve the requested production target.");
                    Equal(typeof(void), method.ReturnType, "Keep the required return type.");
                    Equal(0, method.GetParameters().Length, "Keep the required parameterless signature.");
                    Equal(_publicized, method.IsPublic, "Exercise Android method visibility as well as desktop.");
                }
            }
            finally { context.Unload(); }
        }

        private static object? Invoke(Type patch, string name, params object[] arguments)
        {
            try
            {
                return patch.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public void Dispose()
        {
            Directory.Delete(_directory, recursive: true);
            Equal(_sourceHash, GameIdentityFileHasher.Instance.Sha256(_source),
                "Publicizer and malformed-shape fixtures must leave the original game assembly unchanged.");
        }

        private static string FindPreloadReferenceDirectory()
        {
            for (var parent = new DirectoryInfo(AppContext.BaseDirectory); parent != null; parent = parent.Parent)
            {
                var candidate = Path.Combine(parent.FullName, "upstream", "godot-export", ".godot", "mono", "publish", "arm64");
                if (File.Exists(Path.Combine(candidate, "sts2.dll"))) return candidate;
            }
            throw new FileNotFoundException("The real game reference assembly is required for preload target regression tests.");
        }
    }

    private sealed class PreloadAssemblyContext(string gameAssembly, string[] probeDirectories)
        : AssemblyLoadContext("preload-target-regression", isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name == "sts2")
            {
                // Load fixture bytes without keeping a Windows file mapping
                // alive until the collectible context's next GC.
                using var input = File.OpenRead(gameAssembly);
                return LoadFromStream(input);
            }
            if (name.Name == "netstandard" || name.Name == "mscorlib"
                || name.Name!.StartsWith("System", StringComparison.Ordinal)
                || name.Name.StartsWith("Microsoft.", StringComparison.Ordinal)) return null;
            foreach (var directory in probeDirectories)
            {
                var path = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(path)) return LoadFromAssemblyPath(path);
            }
            return null;
        }
    }
}
