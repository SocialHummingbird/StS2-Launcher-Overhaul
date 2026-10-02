using System.Threading.Tasks;
using Godot;
using System.Collections.Generic;
using System;

namespace STS2Mobile.Launcher;
// Compiles shaders on first launch by collecting materials from resources and scenes,
// rendering them in a SubViewport, then writing a version marker to skip on future launches.
internal sealed partial class ShaderWarmupScreen : Control
{
    private const int WarmupVersion = 9;
    private const int WarmupTimeBudgetSeconds = 90;
    private const int WatchdogWarningSeconds = 45;
    private TaskCompletionSource<bool> _tcs;
    private Label _statusLabel;
    private Label _detailLabel;
    private ProgressBar _progressBar;
    private volatile bool _warmupFinished;
    private bool _previousRenderCrashSuspected;
    internal async Task RunAsync()
    {
        _previousRenderCrashSuspected = PreviousWarmupStatusSuggestsRenderCrash();
        _tcs = new TaskCompletionSource<bool>();
        Initialize();
        await _tcs.Task;
    }

    private async Task RunWarmupAsync(LauncherMonotonicDeadline deadline)
    {
        bool previousRenderCrashSuspected = _previousRenderCrashSuspected;
        var warmup = CreateWarmupRun(deadline);
        WriteWarmupStatus("collecting", "Collecting shader warmup materials");
        var scan = await CollectWarmupMaterialsAsync(warmup.Tree, warmup.Progress, warmup.Deadline);
        var materials = scan.Materials;
        if (warmup.IsOverBudget)
        {
            warmup.CompletePartialAndReport(0, materials.Count);
            MarkWarmupComplete();
            WriteWarmupStatus("completed-deadline", $"Shader warmup reached its {WarmupTimeBudgetSeconds}s deadline after collecting {materials.Count} materials; continuing startup", MergeEvidence(new[] { $"Elapsed ms: {warmup.ElapsedMilliseconds}", "Classification: hard deadline reached during resource scan", }, scan.Diagnostics.ToEvidenceLines()));
            PatchHelper.Log(Message.TimeBudgetReached(WarmupTimeBudgetSeconds));
            return;
        }

        if (materials.Count == 0)
        {
            WriteWarmupStatus("completed", "No shader warmup materials found", scan.Diagnostics.ToEvidenceLines());
            MarkWarmupComplete();
            return;
        }

        var renderPlan = ShaderWarmupRenderPlan.ForMaterialCount(materials.Count, previousRenderCrashSuspected);
        WriteWarmupStatus("rendering", $"Rendering {renderPlan.TargetMaterialCount} of {materials.Count} shader warmup materials using plan {renderPlan.Name}", renderPlan.ToEvidenceLines());
        int rendered = await RenderWarmupMaterialsAsync(warmup.Tree, warmup.Progress, materials, renderPlan, warmup.Deadline);
        if (rendered < materials.Count)
        {
            warmup.CompletePartialAndReport(rendered, materials.Count);
            MarkWarmupComplete();
            WriteWarmupStatus("completed-partial", $"Rendered {rendered} of {materials.Count} shader warmup materials before the {WarmupTimeBudgetSeconds}s budget", MergeEvidence(new[] { $"Elapsed ms: {warmup.ElapsedMilliseconds}", $"Classification: {renderPlan.CompletionClassification()}", }, renderPlan.ToEvidenceLines(), scan.Diagnostics.ToEvidenceLines()));
            return;
        }

        warmup.CompleteAndReport(rendered);
        MarkWarmupComplete();
        WriteWarmupStatus("completed", $"Rendered {rendered} shader warmup materials", MergeEvidence(new[] { $"Elapsed ms: {warmup.ElapsedMilliseconds}", $"Classification: {renderPlan.CompletionClassification()}", }, renderPlan.ToEvidenceLines(), scan.Diagnostics.ToEvidenceLines()));
    }

    private async Task<ShaderWarmupMaterialScanner.ShaderWarmupMaterialScanResult> CollectWarmupMaterialsAsync(SceneTree tree, ShaderWarmupProgress progress, LauncherMonotonicDeadline deadline)
    {
        progress.ShowScanning();
        WriteWarmupStatus("waiting-post-draw", "Waiting before shader resource scan");
        await WaitPostDrawAsync(deadline);
        var materials = await ShaderWarmupMaterialScanner.CollectAsync(tree, progress, deadline);
        PatchHelper.Log(Message.Collected(materials.Materials.Count));
        WriteWarmupStatus("collected", $"Collected {materials.Materials.Count} unique shader warmup materials", materials.Diagnostics.ToEvidenceLines());
        return materials;
    }

    private async Task<int> RenderWarmupMaterialsAsync(SceneTree tree, ShaderWarmupProgress progress, List<WarmupMaterial> materials, ShaderWarmupRenderPlan renderPlan, LauncherMonotonicDeadline deadline)
    {
        progress.ShowCompiling();
        var renderer = ShaderWarmupRenderer.ForScreen(this, tree, progress, renderPlan, deadline);
        return await renderer.RenderAsync(materials);
    }

    private static void MarkWarmupComplete() => WriteWarmupVersion();
    private readonly struct ShaderWarmupRenderPlan
    {
        private const int DefaultBatchSize = 8;
        private const int AndroidBatchSize = 1;
        private const int AndroidLargeMaterialThreshold = 512;
        private const int AndroidLargeMaterialLimit = 128;
        private const int PreviousRenderCrashLimit = 64;
        private ShaderWarmupRenderPlan(string name, string reason, int totalMaterialCount, int targetMaterialCount, int batchSize, bool previousRenderCrashSuspected)
        {
            Name = name;
            Reason = reason;
            TotalMaterialCount = totalMaterialCount;
            TargetMaterialCount = targetMaterialCount;
            BatchSize = batchSize;
            PreviousRenderCrashSuspected = previousRenderCrashSuspected;
        }

        internal string Name { get; }
        internal string Reason { get; }
        internal int TotalMaterialCount { get; }
        internal int TargetMaterialCount { get; }
        internal int BatchSize { get; }
        internal bool PreviousRenderCrashSuspected { get; }
        internal bool IsPartial => TargetMaterialCount < TotalMaterialCount;

        internal static ShaderWarmupRenderPlan ForMaterialCount(int totalMaterialCount, bool previousRenderCrashSuspected)
        {
            int normalizedTotal = Math.Max(0, totalMaterialCount);
            if (OperatingSystem.IsAndroid() && previousRenderCrashSuspected)
            {
                return Create("android-render-crash-recovery", "Previous shader warmup marker stopped during rendering; using a minimal compatibility warmup to avoid a crash loop", normalizedTotal, PreviousRenderCrashLimit, AndroidBatchSize, previousRenderCrashSuspected);
            }

            if (OperatingSystem.IsAndroid() && normalizedTotal > AndroidLargeMaterialThreshold)
            {
                return Create("android-bounded-large-shader-set", "Android shader set is large; using bounded compatibility warmup so startup is not blocked by driver/resource pressure", normalizedTotal, AndroidLargeMaterialLimit, AndroidBatchSize, previousRenderCrashSuspected);
            }

            return Create("full", "Full shader warmup selected", normalizedTotal, normalizedTotal, DefaultBatchSize, previousRenderCrashSuspected);
        }

        internal string[] ToEvidenceLines() => [$"Render plan: {Name}", $"Render plan reason: {Reason}", $"Render target materials: {TargetMaterialCount}/{TotalMaterialCount}", $"Render batch size: {BatchSize}", $"Previous render crash suspected: {PreviousRenderCrashSuspected}", ];
        internal string CompletionClassification() => IsPartial ? $"{Name}; partial compatibility warmup completed; startup continued" : "full shader warmup completed";
        private static ShaderWarmupRenderPlan Create(string name, string reason, int totalMaterialCount, int requestedTargetMaterialCount, int batchSize, bool previousRenderCrashSuspected)
        {
            int target = Math.Min(Math.Max(0, requestedTargetMaterialCount), totalMaterialCount);
            return new ShaderWarmupRenderPlan(name, reason, totalMaterialCount, target, Math.Max(1, batchSize), previousRenderCrashSuspected);
        }
    }

    private void Initialize()
    {
        ZIndex = 100;
        WriteWarmupStatus("initializing", "Building shader warmup screen");
        try
        {
            var vpSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = vpSize;
            BuildUI(vpSize);
            PatchHelper.Log(Message.ScreenInitialized);
        }
        catch (Exception ex)
        {
            WriteWarmupStatus("ui-build-failed", ex.GetBaseException().Message);
            PatchHelper.Log(Message.ScreenBuildFailed(ex));
            _tcs?.TrySetResult(false);
            return;
        }

        Callable.From(RunWarmup).CallDeferred();
    }

    private void RunWarmup() => _ = RunWarmupTaskAsync();
    private async Task RunWarmupTaskAsync()
    {
        _warmupFinished = false;
        _ = WatchWarmupDurationAsync();
        var deadline = LauncherMonotonicDeadline.Start(TimeSpan.FromSeconds(WarmupTimeBudgetSeconds));
        try
        {
            WriteWarmupStatus("running", "Shader warmup task started");
            await RunWarmupAsync(deadline);
        }
        catch (Exception ex)
        {
            WriteWarmupStatus("failed", ex.GetBaseException().Message);
            PatchHelper.Log(Message.RunFailed(ex));
        }
        finally
        {
            _warmupFinished = true;
        }

        _tcs?.TrySetResult(true);
    }

    private async Task WatchWarmupDurationAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(WatchdogWarningSeconds));
        if (_warmupFinished)
            return;
        WriteWarmupStatus("watchdog-warning", $"Shader warmup still active after {WatchdogWarningSeconds}s");
        PatchHelper.Log(Message.WatchdogWarning(WatchdogWarningSeconds));
    }

    private async Task WaitPostDrawAsync(LauncherMonotonicDeadline deadline)
    {
        await LauncherAsyncYield.FramePostDrawAsync(deadline);
    }

    private readonly struct WarmupCompletion
    {
        internal WarmupCompletion(int materialCount, long elapsedMilliseconds)
        {
            MaterialCount = materialCount;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        internal int MaterialCount { get; }
        internal long ElapsedMilliseconds { get; }
    }

    private readonly struct WarmupPartialCompletion
    {
        internal WarmupPartialCompletion(int renderedMaterialCount, int totalMaterialCount, long elapsedMilliseconds)
        {
            RenderedMaterialCount = renderedMaterialCount;
            TotalMaterialCount = totalMaterialCount;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        internal int RenderedMaterialCount { get; }
        internal int TotalMaterialCount { get; }
        internal long ElapsedMilliseconds { get; }
    }

    private readonly struct WarmupRun
    {
        internal WarmupRun(SceneTree tree, ShaderWarmupProgress progress, LauncherMonotonicDeadline deadline)
        {
            Tree = tree;
            Progress = progress;
            Deadline = deadline;
        }

        internal SceneTree Tree { get; }
        internal ShaderWarmupProgress Progress { get; }
        internal LauncherMonotonicDeadline Deadline { get; }
        internal long ElapsedMilliseconds => Deadline.ElapsedMilliseconds;
        internal bool IsOverBudget => Deadline.IsExpired;

        internal void CompleteAndReport(int materialCount)
        {
            var completion = new WarmupCompletion(materialCount, Deadline.ElapsedMilliseconds);
            Progress.Complete(completion);
            PatchHelper.Log(Message.Completed(completion));
        }

        internal void CompletePartialAndReport(int renderedMaterialCount, int totalMaterialCount)
        {
            var completion = new WarmupPartialCompletion(renderedMaterialCount, totalMaterialCount, Deadline.ElapsedMilliseconds);
            Progress.Complete(new WarmupCompletion(renderedMaterialCount, completion.ElapsedMilliseconds));
            PatchHelper.Log(Message.CompletedPartial(completion));
        }
    }

    private WarmupRun CreateWarmupRun(LauncherMonotonicDeadline deadline) => new(GetTree(), CreateProgress(), deadline);
    private ShaderWarmupProgress CreateProgress() => ShaderWarmupProgress.ForLabels(_statusLabel, _detailLabel, _progressBar);
}
