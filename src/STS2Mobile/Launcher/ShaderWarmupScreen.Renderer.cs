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
            ShaderWarmupProgress progress
        )
        {
            _parent = parent;
            _tree = tree;
            _progress = progress;
        }

        internal static ShaderWarmupRenderer ForScreen(
            Control parent,
            SceneTree tree,
            ShaderWarmupProgress progress
        )
            => new(parent, tree, progress);

        internal async Task RenderAsync(List<WarmupMaterial> materials)
        {
            var viewport = CreateViewport();
            _parent.AddChild(viewport);
            try
            {
                await RenderBatchesAsync(viewport, CreateWhiteTexture(), materials);
            }
            finally
            {
                viewport.QueueFree();
            }
        }

        private async Task RenderBatchesAsync(
            SubViewport viewport,
            ImageTexture whiteTexture,
            List<WarmupMaterial> materials
        )
        {
            int total = materials.Count;
            for (int i = 0; i < total; i += BatchSize)
            {
                var batch = new WarmupRenderBatch(
                    i,
                    Math.Min(i + BatchSize, total)
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
            }
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
