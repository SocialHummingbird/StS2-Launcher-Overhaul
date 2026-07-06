using System.Diagnostics;
using System;
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

    private readonly struct WarmupPartialCompletion
    {
        internal WarmupPartialCompletion(
            int renderedMaterialCount,
            int totalMaterialCount,
            long elapsedMilliseconds
        )
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

        internal long ElapsedMilliseconds => Stopwatch.ElapsedMilliseconds;

        internal bool IsOverBudget
            => Stopwatch.Elapsed >= TimeSpan.FromSeconds(WarmupTimeBudgetSeconds);

        internal void CompleteAndReport(int materialCount)
        {
            var completion = new WarmupCompletion(
                materialCount,
                Stopwatch.ElapsedMilliseconds
            );
            Progress.Complete(completion);
            PatchHelper.Log(Message.Completed(completion));
        }

        internal void CompletePartialAndReport(int renderedMaterialCount, int totalMaterialCount)
        {
            var completion = new WarmupPartialCompletion(
                renderedMaterialCount,
                totalMaterialCount,
                Stopwatch.ElapsedMilliseconds
            );
            Progress.Complete(
                new WarmupCompletion(
                    renderedMaterialCount,
                    completion.ElapsedMilliseconds
                )
            );
            PatchHelper.Log(Message.CompletedPartial(completion));
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
