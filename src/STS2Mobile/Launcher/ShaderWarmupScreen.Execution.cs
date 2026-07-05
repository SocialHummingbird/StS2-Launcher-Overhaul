using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private async Task RunWarmupAsync()
    {
        var warmup = CreateWarmupRun();
        WriteWarmupStatus("collecting", "Collecting shader warmup materials");

        var materials = await CollectWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress
        );

        if (materials.Count == 0)
        {
            WriteWarmupStatus("completed", "No shader warmup materials found");
            MarkWarmupComplete();
            return;
        }

        WriteWarmupStatus("rendering", $"Rendering {materials.Count} shader warmup materials");
        await RenderWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress,
            materials
        );

        warmup.CompleteAndReport(materials.Count);
        MarkWarmupComplete();
        WriteWarmupStatus("completed", $"Rendered {materials.Count} shader warmup materials");
        await WaitFinishDelayAsync();
    }

    private async Task<List<WarmupMaterial>> CollectWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress
    )
    {
        progress.ShowScanning();
        WriteWarmupStatus("waiting-post-draw", "Waiting before shader resource scan");
        await WaitPostDrawAsync();

        var materials = await ShaderWarmupMaterialScanner.CollectAsync(tree, progress);
        PatchHelper.Log(Message.Collected(materials.Count));
        return materials;
    }

    private async Task RenderWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress,
        List<WarmupMaterial> materials
    )
    {
        progress.ShowCompiling();
        var renderer = ShaderWarmupRenderer.ForScreen(this, tree, progress);
        await renderer.RenderAsync(materials);
    }

    private static void MarkWarmupComplete()
        => WriteWarmupVersion();
}
