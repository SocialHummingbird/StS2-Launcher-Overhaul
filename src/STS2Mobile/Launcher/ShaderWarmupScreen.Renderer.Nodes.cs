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
            List<(string Path, Material Material)> materials,
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
                    Node node = CreateNode(material.Material, whiteTexture);
                    if (node != null)
                    {
                        viewport.AddChild(node);
                        batchNodes.Add(node);
                    }
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"[ShaderWarmup] Failed to create node for {material.Path}: {ex.Message}");
                }
            }

            return batchNodes;
        }

        private static Node CreateNode(Material mat, ImageTexture whiteTexture)
            => mat is ParticleProcessMaterial particleMat
                ? new GpuParticles2D
                {
                    ProcessMaterial = particleMat,
                    Amount = ParticleAmount,
                    Emitting = true,
                    OneShot = false,
                    Texture = whiteTexture,
                }
                : new Sprite2D
                {
                    Texture = whiteTexture,
                    Material = mat,
                };
    }
}
