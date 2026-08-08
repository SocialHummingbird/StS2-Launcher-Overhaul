using System;
using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed partial class ShaderWarmupRenderer
    {
        private static List<Node> AddBatchNodes(
            SubViewport viewport,
            ImageTexture whiteTexture,
            List<WarmupMaterial> materials,
            WarmupRenderBatch batch
        )
        {
            var batchNodes = new List<Node>();
            for (int i = batch.Start; i < batch.End; i++)
            {
                var material = materials[i];
                try
                {
                    Node node = material.CreateNode(whiteTexture);
                    if (node != null)
                    {
                        viewport.AddChild(node);
                        batchNodes.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    material.LogNodeCreationFailed(ex);
                }
            }

            return batchNodes;
        }
    }
}
