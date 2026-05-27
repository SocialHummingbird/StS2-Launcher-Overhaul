using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using STS2Mobile.Launcher;

namespace STS2Mobile;

// Entry point for the mobile patcher. Bootstraps GodotSharp, applies all Harmony
// patches, and falls back to standalone launcher mode if game files aren't present.
public static class ModEntry
{
    private const string HarmonyId = "com.sts2mobile";
    private const int NotStarted = 0;
    private const int InProgress = 1;
    private const int Complete = 2;

    private static int _applyState = NotStarted;
    private static int _exceptionHandlersInstalled;

    // Bootstraps GodotSharp by setting up DLL import resolver, native interop,
    // and managed callbacks. Called from gd_mono.cpp before Apply().
    [UnmanagedCallersOnly]
    public static int InitializeGodotSharp(
        IntPtr godotDllHandle,
        IntPtr outManagedCallbacks,
        IntPtr unmanagedCallbacks,
        int unmanagedCallbacksSize
    )
    {
        try
        {
            DllImportResolver dllImportResolver = new GodotDllImportResolver(godotDllHandle).OnResolveDllImport;
            var coreApiAssembly = typeof(GodotObject).Assembly;
            NativeLibrary.SetDllImportResolver(coreApiAssembly, dllImportResolver);

            NativeFuncs.Initialize(unmanagedCallbacks, unmanagedCallbacksSize);
            ManagedCallbacks.Create(outManagedCallbacks);

            return 1;
        }
        catch
        {
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    public static void Apply()
    {
        ApplyInternal();
    }

    [UnmanagedCallersOnly]
    public static int ApplyFromGodot()
    {
        try
        {
            BootstrapTrace.Log("ApplyFromGodot entered");
            var applyInternal = typeof(ModEntry).GetMethod(
                "ApplyInternal",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            if (applyInternal == null)
            {
                BootstrapTrace.Log("ApplyInternal reflection lookup failed");
                return -2;
            }

            applyInternal.Invoke(null, null);
            BootstrapTrace.Log("ApplyFromGodot completed");
            return 0;
        }
        catch (Exception ex)
        {
            BootstrapTrace.Log($"Unhandled bootstrap failure: {ex}");
            return -1;
        }
    }

    [UnmanagedCallersOnly]
    public static int BootstrapProbe()
    {
        return 1729;
    }

    [UnmanagedCallersOnly]
    public static int HarmonyConstructorProbe()
    {
        _ = new Harmony(HarmonyId);
        return 1730;
    }

    [UnmanagedCallersOnly]
    public static int ShowLauncherOnly()
    {
        try
        {
            ScheduleStandaloneLauncher();
            return 1;
        }
        catch
        {
            return 0;
        }
    }

    private static void ApplyInternal()
    {
        BootstrapTrace.Log("ApplyInternal entered");
        InstallManagedExceptionHandlers();
        if (Interlocked.CompareExchange(ref _applyState, InProgress, NotStarted) != NotStarted)
        {
            BootstrapTrace.Log("ApplyInternal duplicate invocation skipped");
            PatchHelper.Log("Apply already running/completed; skipping duplicate invocation.");
            return;
        }

        try
        {
            ApplyStartupPatches();
        }
        finally
        {
            _applyState = Complete;
        }
    }

    private static void ApplyStartupPatches()
    {
        BootstrapTrace.Log("Initializing STS2Mobile");
        PatchHelper.Log("Initializing STS2Mobile...");
        try
        {
            ConfigureWritableTempDirectory();
            var harmony = new Harmony(HarmonyId);
            BootstrapTrace.Log("Starting startup patch orchestration");
            var patchResult = StartupPatchOrchestrator.Apply(harmony);
            BootstrapTrace.Log("Finished startup patch orchestration");

            if (patchResult.CriticalFailed)
            {
                PatchHelper.Log("Critical startup patches failed; scheduling standalone launcher fallback.");
                ScheduleStandaloneLauncher();
                return;
            }

            if (patchResult.HasFailures)
            {
                PatchHelper.Log(
                    $"Startup completed with {patchResult.FailedPatchCount} non-critical patch failures."
                );
            }

            foreach (var failure in patchResult.FailureMessages().Take(10))
            {
                PatchHelper.Log($"[startup] {failure}");
            }

            PatchHelper.Log("Startup patch orchestration complete.");
            if (IsLauncherOnlyMode())
            {
                ScheduleStandaloneLauncher();
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Unexpected startup error: {ex.Message}");
            ScheduleStandaloneLauncher();
        }
    }

    private static void InstallManagedExceptionHandlers()
    {
        if (Interlocked.Exchange(ref _exceptionHandlersInstalled, 1) == 1)
            return;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            try
            {
                BootstrapTrace.Log($"Unhandled managed exception: {args.ExceptionObject}");
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            try
            {
                BootstrapTrace.Log($"Unobserved task exception: {args.Exception}");
            }
            catch { }
        };

        BootstrapTrace.Log("Managed exception handlers installed");
    }

    private static void ConfigureWritableTempDirectory()
    {
        var tempDir = ResolveManagedTempDirectory();
        Directory.CreateDirectory(tempDir);

        System.Environment.SetEnvironmentVariable("TMPDIR", tempDir);
        System.Environment.SetEnvironmentVariable("TMP", tempDir);
        System.Environment.SetEnvironmentVariable("TEMP", tempDir);
        PatchHelper.Log($"Using writable temp directory: {tempDir}");
    }

    private static string ResolveManagedTempDirectory()
    {
        return "/data/data/com.sts2launcher.overhaul.fork.dev/files/tmp";
    }

    private static bool IsLauncherOnlyMode()
    {
        return !IsGamePckStructurallyReady(
            "/data/data/com.sts2launcher.overhaul.fork.dev/files/game/SlayTheSpire2.pck"
        );
    }

    private static bool IsGamePckStructurallyReady(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            using var fs = File.OpenRead(path);
            using var reader = new BinaryReader(fs);
            if (fs.Length < 96)
                return false;

            if (reader.ReadUInt32() != 0x43504447)
                return false;

            reader.ReadUInt32(); // format version
            reader.ReadUInt32(); // major
            reader.ReadUInt32(); // minor
            reader.ReadUInt32(); // patch
            reader.ReadUInt32(); // flags
            reader.ReadInt64(); // file base
            var dirBase = reader.ReadInt64();
            if (dirBase <= 0 || dirBase + 4 > fs.Length)
                return false;

            fs.Position = dirBase;
            return reader.ReadUInt32() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void ScheduleStandaloneLauncher()
    {
        PatchHelper.Log("Scheduling standalone launcher...");
        Callable.From(CreateStandaloneLauncher).CallDeferred();
    }

    private static void CreateStandaloneLauncher()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            Callable.From(CreateStandaloneLauncher).CallDeferred();
            return;
        }

        var launcher = new LauncherUI();
        tree.Root.AddChild(launcher);
        launcher.Initialize();
        PatchHelper.Log("Standalone launcher displayed");
    }
}
