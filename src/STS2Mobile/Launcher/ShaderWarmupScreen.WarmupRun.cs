using System.Diagnostics;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
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
}
