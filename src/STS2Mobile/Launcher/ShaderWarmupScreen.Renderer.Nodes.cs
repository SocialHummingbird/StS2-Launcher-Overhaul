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
            int start,
            int end
        )
        {
            var batchNodes = new List<Node>();
            for (int i = start; i < end; i++)
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
