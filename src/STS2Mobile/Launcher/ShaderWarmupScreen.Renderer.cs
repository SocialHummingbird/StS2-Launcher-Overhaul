using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed partial class ShaderWarmupRenderer
    {
        private const int TextureHeight = 1;
        private const int TextureWidth = 1;
        private const int ViewportHeight = 64;
        private const int ViewportWidth = 64;

        private readonly Control _parent;
        private readonly SceneTree _tree;
        private readonly ShaderWarmupProgress _progress;
        private readonly ShaderWarmupRenderPlan _renderPlan;
        private readonly LauncherMonotonicDeadline _deadline;

        private readonly struct WarmupRenderBatch
        {
            internal WarmupRenderBatch(int start, int end)
            {
                Start = start;
                End = end;
            }

            internal int Start { get; }
            internal int End { get; }
        }

        private ShaderWarmupRenderer(
            Control parent,
            SceneTree tree,
            ShaderWarmupProgress progress,
            ShaderWarmupRenderPlan renderPlan,
            LauncherMonotonicDeadline deadline
        )
        {
            _parent = parent;
            _tree = tree;
            _progress = progress;
            _renderPlan = renderPlan;
            _deadline = deadline;
        }

        internal static ShaderWarmupRenderer ForScreen(
            Control parent,
            SceneTree tree,
            ShaderWarmupProgress progress,
            ShaderWarmupRenderPlan renderPlan,
            LauncherMonotonicDeadline deadline
        )
            => new(parent, tree, progress, renderPlan, deadline);

        internal async Task<int> RenderAsync(List<WarmupMaterial> materials)
        {
            var viewport = CreateViewport();
            _parent.AddChild(viewport);
            try
            {
                return await RenderBatchesAsync(viewport, CreateWhiteTexture(), materials);
            }
            finally
            {
                viewport.QueueFree();
            }
        }

        private async Task<int> RenderBatchesAsync(
            SubViewport viewport,
            ImageTexture whiteTexture,
            List<WarmupMaterial> materials
        )
        {
            int total = materials.Count;
            int target = _renderPlan.TargetMaterialCount;
            int rendered = 0;
            for (int i = 0; i < target; i += _renderPlan.BatchSize)
            {
                if (_deadline.IsExpired)
                {
                    PatchHelper.Log(Message.TimeBudgetReached(WarmupTimeBudgetSeconds));
                    return rendered;
                }

                var batch = new WarmupRenderBatch(
                    i,
                    Math.Min(i + _renderPlan.BatchSize, target)
                );
                WriteWarmupStatus(
                    "rendering-batch",
                    $"Rendering shader warmup materials {batch.Start + 1}-{batch.End} of {total} using plan {_renderPlan.Name}",
                    _renderPlan.ToEvidenceLines()
                );
                var batchNodes = AddBatchNodes(
                    viewport,
                    whiteTexture,
                    materials,
                    batch
                );
                try
                {
                    ReportProgress(batch.End, total);
                    if (!await WaitForRenderFramesAsync())
                        return rendered;
                }
                finally
                {
                    ClearBatch(batchNodes);
                }
                rendered = batch.End;
            }

            return rendered;
        }

        private async Task<bool> WaitForRenderFramesAsync()
        {
            if (_tree == null)
                return !_deadline.IsExpired;

            if (!await LauncherAsyncYield.ProcessFrameAsync(_tree, _deadline))
                return false;
            return await LauncherAsyncYield.ProcessFrameAsync(_tree, _deadline);
        }

        private static void ClearBatch(List<Node> nodes)
        {
            foreach (var node in nodes)
                node.QueueFree();
        }

        private void ReportProgress(int completed, int total)
            => _progress.ReportCompileProgress(completed, total);
    }
}
