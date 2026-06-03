using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

// Compiles shaders on first launch by collecting materials from resources and scenes,
// rendering them in a SubViewport, then writing a version marker to skip on future launches.
internal sealed partial class ShaderWarmupScreen : Control
{
    private const int WarmupVersion = 5;
    private const int WarmupParticleAmount = 1;

    private readonly struct WarmupMaterial
    {
        internal WarmupMaterial(string path, Material material)
        {
            Path = path;
            Material = material;
        }

        private string Path { get; }
        private Material Material { get; }

        internal Node CreateNode(ImageTexture whiteTexture)
            => Material is ParticleProcessMaterial particleMat
                ? new GpuParticles2D
                {
                    ProcessMaterial = particleMat,
                    Amount = WarmupParticleAmount,
                    Emitting = true,
                    OneShot = false,
                    Texture = whiteTexture,
                }
                : new Sprite2D
                {
                    Texture = whiteTexture,
                    Material = Material,
                };

        internal void LogNodeCreationFailed(Exception ex)
            => PatchHelper.Log($"[ShaderWarmup] Failed to create node for {Path}: {ex.Message}");
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
        var sw = Stopwatch.StartNew();
        var progress = new ShaderWarmupProgress(_statusLabel, _detailLabel, _progressBar);

        progress.ShowScanning();
        await WaitPostDrawAsync();

        var tree = GetTree();
        var materials = await ShaderWarmupMaterialScanner.CollectAsync(tree, progress);
        PatchHelper.Log(Message.Collected(materials.Count));

        progress.ShowCompiling();

        if (materials.Count == 0)
        {
            MarkWarmupComplete();
            return;
        }

        var renderer = new ShaderWarmupRenderer(this, tree, progress);
        await renderer.RenderAsync(materials);

        var elapsedMilliseconds = sw.ElapsedMilliseconds;
        progress.Complete(materials.Count, elapsedMilliseconds);
        PatchHelper.Log(Message.Completed(materials.Count, elapsedMilliseconds));

        MarkWarmupComplete();
        await WaitFinishDelayAsync();
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
