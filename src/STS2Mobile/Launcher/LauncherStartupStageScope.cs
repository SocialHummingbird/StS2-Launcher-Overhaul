using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

// A bounded foreground stage. Node exit and launch failure end its waits.
internal sealed class LauncherStartupStageScope : IDisposable
{
    private readonly LauncherStartupOperation _operation;
    private readonly LauncherMonotonicDeadline _deadline;
    private readonly LauncherOperationLifecycle _lifecycle = new();
    private readonly LauncherOperationLifecycleMonitor _monitor;
    private readonly LauncherAsyncSignalWaiter _waiter;
    private readonly SceneTree _tree;

    internal LauncherStartupStageScope(Node node, LauncherStartupOperation operation, TimeSpan budget)
    {
        _operation = operation ?? throw new InvalidOperationException("No active launch operation.");
        _tree = node.GetTree() ?? throw new OperationCanceledException("Game window is no longer available.");
        _deadline = LauncherMonotonicDeadline.Start(budget);
        _monitor = new LauncherOperationLifecycleMonitor(_lifecycle, _deadline);
        node.AddChild(_monitor);
        if (OperatingSystem.IsAndroid())
            _monitor.ObserveApplicationActive(!LauncherHandoffVisibility.Capture().SuspendsHandoffTimeout);
        _waiter = LauncherAsyncYield.CreateWaiter(_tree, _lifecycle);
    }

    internal Task WaitAsync(Task task) => _operation.WaitAsync(task, _waiter, _deadline);
    internal Task ProcessFrameAsync() => WaitAsync(ProcessFrameSignalAsync());
    internal Task PostDrawAsync() => WaitAsync(PostDrawSignalAsync());

    private async Task ProcessFrameSignalAsync() => await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
    private static async Task PostDrawSignalAsync()
        => await RenderingServer.Singleton.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

    public void Dispose()
    {
        _lifecycle.Destroy();
        if (GodotObject.IsInstanceValid(_monitor)) _monitor.QueueFree();
    }
}
