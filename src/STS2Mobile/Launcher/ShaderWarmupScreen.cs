using System.Threading.Tasks;
using Godot;

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
}
