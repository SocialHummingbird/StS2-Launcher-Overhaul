using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using STS2Mobile.Launcher;

namespace STS2Mobile;

// Entry point for the mobile patcher. Bootstraps GodotSharp, applies all Harmony
// patches, and falls back to standalone launcher mode if game files aren't present.
public static class ModEntry
{
    private const int ApplyComplete = 2;
    private const int ApplyInProgress = 1;
    private const int ApplyNotStarted = 0;
    private const int BootstrapProbeCode = 1729;
    private const string HarmonyId = "com.sts2mobile";
    private const int HarmonyConstructorProbeCode = 1730;
    private const int ProbeFailure = -1;
    private const int ProbeSuccess = 0;
    private const int ProbeSuccessWithValue = 1;
    private const uint GodotPckMagic = 0x43504447;
    private const string GameBranchFileName = "game_branch";
    private const string GameDirectoryName = "game";
    private const string GameVersionsDirectoryName = "game_versions";
    private const string GamePckFileName = "SlayTheSpire2.pck";
    private const string LauncherBootstrapVariable = "STS2_LAUNCHER_BOOTSTRAP";
    private const int StartupFallbackShieldZIndex = 4090;
    private const int StartupFallbackLauncherZIndex = 4092;
    private const int MinimumPckHeaderLength = 96;
    private const string TempDirectoryName = "tmp";
    private static readonly string[] TempVariableNames =
    {
        "TMPDIR",
        "TMP",
        "TEMP",
    };
    private static int _applyState = ApplyNotStarted;
    private static int _exceptionHandlersInstalled;
    private static string _startupFallbackReason;

    internal static bool HasStartupFallbackReason
        => !string.IsNullOrWhiteSpace(_startupFallbackReason);

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
            GodotSharpBootstrap.Initialize(
                godotDllHandle,
                outManagedCallbacks,
                unmanagedCallbacks,
                unmanagedCallbacksSize
            );
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
            ApplyInternal();
            BootstrapTrace.Log("ApplyFromGodot completed");
            return ProbeSuccess;
        }
        catch (Exception ex)
        {
            BootstrapTrace.Log($"Unhandled bootstrap failure: {ex}");
            return ProbeFailure;
        }
    }

    [UnmanagedCallersOnly]
    public static int BootstrapProbe()
    {
        return BootstrapProbeCode;
    }

    [UnmanagedCallersOnly]
    public static int HarmonyConstructorProbe()
    {
        _ = new Harmony(HarmonyId);
        return HarmonyConstructorProbeCode;
    }

    [UnmanagedCallersOnly]
    public static int ShowLauncherOnly()
    {
        try
        {
            ScheduleStandaloneLauncher();
            return ProbeSuccessWithValue;
        }
        catch
        {
            return ProbeSuccess;
        }
    }

    private static void ApplyInternal()
    {
        BootstrapTrace.Log("ApplyInternal entered");
        InstallManagedExceptionHandlers();
        if (!TryBeginApply())
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
            CompleteApply();
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
                var reason = "Critical startup patches failed:\n"
                    + string.Join("\n", patchResult.FailureMessages().Take(5));
                PatchHelper.Log("Critical startup patches failed; scheduling standalone launcher fallback.");
                ScheduleStandaloneLauncher(reason);
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
            if (IsLauncherBootstrapRequested())
            {
                PatchHelper.Log("Launcher bootstrap mode requested; showing launcher UI.");
                ScheduleStandaloneLauncher();
            }
            else if (IsStandaloneLauncherRequired())
            {
                ScheduleStandaloneLauncher("Game files are not ready. Launcher-only mode started.");
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Unexpected startup error: {ex.Message}");
            ScheduleStandaloneLauncher($"Unexpected startup error:\n{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool IsStandaloneLauncherRequired()
        => !IsGamePckStructurallyReady(GamePckPath);

    private static bool IsLauncherBootstrapRequested()
        => string.Equals(
            System.Environment.GetEnvironmentVariable(LauncherBootstrapVariable),
            "1",
            StringComparison.Ordinal
        );

    private static string GamePckPath
        => Path.Combine(
            GameDirectoryPath,
            GamePckFileName
        );

    private static string GameDirectoryPath
    {
        get
        {
            var branch = ReadSelectedBranch();
            return string.Equals(branch, "public", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(RuntimeDataDirectory, GameDirectoryName)
                : Path.Combine(RuntimeDataDirectory, GameVersionsDirectoryName, StateDirectoryName(branch), GameDirectoryName);
        }
    }

    private static string ManagedTempDirectory
        => Path.Combine(RuntimeDataDirectory, TempDirectoryName);

    private static string ReadSelectedBranch()
    {
        try
        {
            var path = Path.Combine(RuntimeDataDirectory, GameBranchFileName);
            if (!File.Exists(path))
                return "public";

            var branch = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(branch) ? "public" : branch;
        }
        catch
        {
            return "public";
        }
    }

    private static string StateDirectoryName(string branch)
    {
        branch = StorageIdentity(branch);

        if (string.Equals(branch, "public", StringComparison.OrdinalIgnoreCase))
            return "public";

        if (string.Equals(branch, "beta", StringComparison.OrdinalIgnoreCase))
            return "beta";

        var sb = new System.Text.StringBuilder(branch.Length);
        foreach (var ch in branch)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.')
                sb.Append(ch);
            else
                sb.Append('_');
        }

        var safePrefix = sb.Length == 0 ? "branch" : sb.ToString();
        if (safePrefix.Length > 48)
            safePrefix = safePrefix[..48].TrimEnd('.', '-', '_');

        if (safePrefix.Length == 0)
            safePrefix = "branch";

        return $"{safePrefix}-{StableBranchHash(branch)}";
    }

    private static string StorageIdentity(string branch)
        => string.IsNullOrWhiteSpace(branch) ? "public" : branch.Trim().ToLowerInvariant();

    private static string StableBranchHash(string branch)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            var hash = offsetBasis;
            foreach (var ch in branch)
            {
                hash ^= ch;
                hash *= prime;
            }

            return hash.ToString("x8");
        }
    }

    private static string RuntimeDataDirectory
    {
        get
        {
            try
            {
                var dataDir = OS.GetDataDir();
                if (BootstrapTrace.TryNormalizeDirectory(dataDir, out var normalized))
                    return normalized;
            }
            catch
            {
            }

            return BootstrapTrace.ResolveFallbackDataDirectory();
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
            catch (Exception ex)
            {
                BootstrapTrace.Log($"Managed exception handler logging failed: {ex.Message}");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            try
            {
                BootstrapTrace.Log($"Unobserved task exception: {args.Exception}");
            }
            catch (Exception ex)
            {
                BootstrapTrace.Log($"Managed exception handler logging failed: {ex.Message}");
            }
        };

        BootstrapTrace.Log("Managed exception handlers installed");
    }

    private static void ConfigureWritableTempDirectory()
    {
        Directory.CreateDirectory(ManagedTempDirectory);

        foreach (var variable in TempVariableNames)
            System.Environment.SetEnvironmentVariable(variable, ManagedTempDirectory);

        PatchHelper.Log($"Using writable temp directory: {ManagedTempDirectory}");
    }

    private static bool IsGamePckStructurallyReady(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            using var fs = File.OpenRead(path);
            using var reader = new BinaryReader(fs);
            if (!TryReadPckDirectoryBase(reader, fs.Length, out var dirBase))
                return false;

            fs.Position = dirBase;
            return reader.ReadUInt32() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadPckDirectoryBase(BinaryReader reader, long fileLength, out long dirBase)
    {
        dirBase = 0;
        if (fileLength < MinimumPckHeaderLength)
            return false;

        if (reader.ReadUInt32() != GodotPckMagic)
            return false;

        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadUInt32();
        reader.ReadInt64();
        dirBase = reader.ReadInt64();
        return dirBase > 0 && dirBase + 4 <= fileLength;
    }

    private static void ScheduleStandaloneLauncher()
        => ScheduleStandaloneLauncher(null);

    private static void ScheduleStandaloneLauncher(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
            _startupFallbackReason = reason;

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
        AddStartupFallbackShield(tree);
        tree.Root.AddChild(launcher);
        launcher.Initialize();
        RaiseStartupFallbackLauncher(launcher);
        AddStartupFallbackBanner(tree);
        PatchHelper.Log("Standalone launcher displayed");
    }

    private static void AddStartupFallbackShield(SceneTree tree)
    {
        if (string.IsNullOrWhiteSpace(_startupFallbackReason))
            return;

        var shield = new ColorRect
        {
            Name = "STS2MobileStartupFallbackShield",
            Color = new Color(0.02f, 0.02f, 0.025f, 0.96f),
            ZIndex = StartupFallbackShieldZIndex,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        shield.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        tree.Root.AddChild(shield);
        PatchHelper.Log("Startup fallback shield displayed");
    }

    private static void RaiseStartupFallbackLauncher(LauncherUI launcher)
    {
        if (string.IsNullOrWhiteSpace(_startupFallbackReason))
            return;

        launcher.ZIndex = StartupFallbackLauncherZIndex;
        PatchHelper.Log("Startup fallback launcher raised above shield");
    }

    private static void AddStartupFallbackBanner(SceneTree tree)
    {
        if (string.IsNullOrWhiteSpace(_startupFallbackReason))
            return;

        PatchHelper.Log(
            "Startup fallback raw banner suppressed; launcher diagnostics retain the startup failure detail."
        );
    }

    private static bool TryBeginApply()
    {
        return Interlocked.CompareExchange(ref _applyState, ApplyInProgress, ApplyNotStarted) == ApplyNotStarted;
    }

    private static void CompleteApply()
    {
        _applyState = ApplyComplete;
    }
}
