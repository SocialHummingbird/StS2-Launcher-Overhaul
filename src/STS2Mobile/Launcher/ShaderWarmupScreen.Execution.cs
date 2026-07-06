using System.Collections.Generic;
using System;
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
            warmup.Progress,
            () => warmup.IsOverBudget
        );

        if (materials.Count == 0)
        {
            WriteWarmupStatus("completed", "No shader warmup materials found");
            MarkWarmupComplete();
            return;
        }

        WriteWarmupStatus("rendering", $"Rendering {materials.Count} shader warmup materials");
        int rendered = await RenderWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress,
            materials,
            () => warmup.IsOverBudget
        );

        if (rendered < materials.Count)
        {
            warmup.CompletePartialAndReport(rendered, materials.Count);
            MarkWarmupComplete();
            WriteWarmupStatus(
                "completed-partial",
                $"Rendered {rendered} of {materials.Count} shader warmup materials before the {WarmupTimeBudgetSeconds}s budget",
                $"Elapsed ms: {warmup.ElapsedMilliseconds}",
                "Classification: precompile time budget reached; startup continued"
            );
            await WaitFinishDelayAsync();
            return;
        }

        warmup.CompleteAndReport(rendered);
        MarkWarmupComplete();
        WriteWarmupStatus(
            "completed",
            $"Rendered {rendered} shader warmup materials",
            $"Elapsed ms: {warmup.ElapsedMilliseconds}",
            "Classification: full shader warmup completed"
        );
        await WaitFinishDelayAsync();
    }

    private async Task<List<WarmupMaterial>> CollectWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress,
        Func<bool> shouldStop
    )
    {
        progress.ShowScanning();
        WriteWarmupStatus("waiting-post-draw", "Waiting before shader resource scan");
        await WaitPostDrawAsync();

        var materials = await ShaderWarmupMaterialScanner.CollectAsync(tree, progress, shouldStop);
        PatchHelper.Log(Message.Collected(materials.Count));
        return materials;
    }

    private async Task<int> RenderWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress,
        List<WarmupMaterial> materials,
        Func<bool> shouldStop
    )
    {
        progress.ShowCompiling();
        var renderer = ShaderWarmupRenderer.ForScreen(this, tree, progress, shouldStop);
        return await renderer.RenderAsync(materials);
    }

    private static void MarkWarmupComplete()
        => WriteWarmupVersion();
}
