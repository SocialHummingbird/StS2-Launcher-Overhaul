using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

// Compiles shaders on first launch by collecting materials from resources and scenes,
// rendering them in a SubViewport, then writing a version marker to skip on future launches.
internal sealed partial class ShaderWarmupScreen : Control
{
    private const int WarmupVersion = 5;

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

    private readonly struct WarmupRun
    {
        internal WarmupRun(
            SceneTree tree,
            ShaderWarmupProgress progress,
            Stopwatch stopwatch
        )
        {
            Tree = tree;
            Progress = progress;
            Stopwatch = stopwatch;
        }

        internal SceneTree Tree { get; }
        internal ShaderWarmupProgress Progress { get; }
        private Stopwatch Stopwatch { get; }

        internal void CompleteAndReport(int materialCount)
        {
            var completion = new WarmupCompletion(
                materialCount,
                Stopwatch.ElapsedMilliseconds
            );
            Progress.Complete(completion);
            PatchHelper.Log(Message.Completed(completion));
        }
    }

    private TaskCompletionSource<bool> _tcs;
    private Label _statusLabel;
    private Label _detailLabel;
    private ProgressBar _progressBar;

    internal async Task RunAsync()
    {
        _tcs = new TaskCompletionSource<bool>();
        Initialize();
        await _tcs.Task;
    }

    private void Initialize()
    {
        ZIndex = 100;

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
            PatchHelper.Log(Message.ScreenBuildFailed(ex));
            _tcs?.TrySetResult(false);
            return;
        }

        Callable.From(RunWarmup).CallDeferred();
    }

    private void RunWarmup()
        => _ = RunWarmupTaskAsync();

    private async Task RunWarmupTaskAsync()
    {
        try
        {
            await RunWarmupAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.RunFailed(ex));
        }

        _tcs?.TrySetResult(true);
    }

    private async Task RunWarmupAsync()
    {
        var warmup = CreateWarmupRun();

        var materials = await CollectWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress
        );

        if (materials.Count == 0)
        {
            MarkWarmupComplete();
            return;
        }

        await RenderWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress,
            materials
        );

        warmup.CompleteAndReport(materials.Count);
        MarkWarmupComplete();
        await WaitFinishDelayAsync();
    }

    private WarmupRun CreateWarmupRun()
        => new(
            GetTree(),
            CreateProgress(),
            Stopwatch.StartNew()
        );

    private ShaderWarmupProgress CreateProgress()
        => ShaderWarmupProgress.ForLabels(
            _statusLabel,
            _detailLabel,
            _progressBar
        );

    private async Task<List<WarmupMaterial>> CollectWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress
    )
    {
        progress.ShowScanning();
        await WaitPostDrawAsync();

        var materials = await ShaderWarmupMaterialScanner.CollectAsync(tree, progress);
        PatchHelper.Log(Message.Collected(materials.Count));
        return materials;
    }

    private async Task RenderWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress,
        List<WarmupMaterial> materials
    )
    {
        progress.ShowCompiling();
        var renderer = ShaderWarmupRenderer.ForScreen(this, tree, progress);
        await renderer.RenderAsync(materials);
    }

    private static void MarkWarmupComplete()
        => WriteWarmupVersion();

    private async Task WaitPostDrawAsync()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private async Task WaitFinishDelayAsync()
    {
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
    }
}
