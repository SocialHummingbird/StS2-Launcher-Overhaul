using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private const int WarmupParticleAmount = 1;

    private readonly struct WarmupMaterial
    {
        private WarmupMaterial(string path, Material material)
        {
            Path = path;
            Material = material;
        }

        private string Path { get; }
        private Material Material { get; }

        internal static WarmupMaterial For(string path, Material material)
            => new(path, material);

        internal Node CreateNode(ImageTexture whiteTexture)
            => Material is ParticleProcessMaterial particleMat
                ? new GpuParticles2D
                {
                    ProcessMaterial = particleMat,
                    Amount = WarmupParticleAmount,
                    Emitting = true,
                    OneShot = false,
                    Texture = whiteTexture,
                }
                : new Sprite2D
                {
                    Texture = whiteTexture,
                    Material = Material,
                };

        internal void LogNodeCreationFailed(Exception ex)
            => PatchHelper.Log(
                $"[ShaderWarmup] Failed to create node for {Path}: {ex.Message}"
            );
    }
}
