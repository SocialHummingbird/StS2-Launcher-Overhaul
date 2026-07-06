using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed partial class ShaderWarmupRenderer
    {
        private const int BatchSize = 8;
        private const int TextureHeight = 1;
        private const int TextureWidth = 1;
        private const int ViewportHeight = 64;
        private const int ViewportWidth = 64;

        private readonly Control _parent;
        private readonly SceneTree _tree;
        private readonly ShaderWarmupProgress _progress;
        private readonly Func<bool> _shouldStop;

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
            Func<bool> shouldStop
        )
        {
            _parent = parent;
            _tree = tree;
            _progress = progress;
            _shouldStop = shouldStop;
        }

        internal static ShaderWarmupRenderer ForScreen(
            Control parent,
            SceneTree tree,
            ShaderWarmupProgress progress,
            Func<bool> shouldStop
        )
            => new(parent, tree, progress, shouldStop);

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
            int rendered = 0;
            for (int i = 0; i < total; i += BatchSize)
            {
                if (_shouldStop())
                {
                    PatchHelper.Log(Message.TimeBudgetReached(WarmupTimeBudgetSeconds));
                    return rendered;
                }

                var batch = new WarmupRenderBatch(
                    i,
                    Math.Min(i + BatchSize, total)
                );
                WriteWarmupStatus(
                    "rendering-batch",
                    $"Rendering shader warmup materials {batch.Start + 1}-{batch.End} of {total}"
                );
                var batchNodes = AddBatchNodes(
                    viewport,
                    whiteTexture,
                    materials,
                    batch
                );

                ReportProgress(batch.End, total);

                await WaitForRenderFramesAsync();
                ClearBatch(batchNodes);
                rendered = batch.End;
            }

            return rendered;
        }

        private async Task WaitForRenderFramesAsync()
        {
            if (_tree == null)
                return;

            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
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
