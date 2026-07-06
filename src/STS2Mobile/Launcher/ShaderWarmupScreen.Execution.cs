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

        var scan = await CollectWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress,
            () => warmup.IsOverBudget
        );
        var materials = scan.Materials;

        if (materials.Count == 0)
        {
            WriteWarmupStatus(
                "completed",
                "No shader warmup materials found",
                scan.Diagnostics.ToEvidenceLines()
            );
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
                MergeEvidence(
                    new[]
                    {
                        $"Elapsed ms: {warmup.ElapsedMilliseconds}",
                        "Classification: precompile time budget reached; startup continued",
                    },
                    scan.Diagnostics.ToEvidenceLines()
                )
            );
            await WaitFinishDelayAsync();
            return;
        }

        warmup.CompleteAndReport(rendered);
        MarkWarmupComplete();
        WriteWarmupStatus(
            "completed",
            $"Rendered {rendered} shader warmup materials",
            MergeEvidence(
                new[]
                {
                    $"Elapsed ms: {warmup.ElapsedMilliseconds}",
                    "Classification: full shader warmup completed",
                },
                scan.Diagnostics.ToEvidenceLines()
            )
        );
        await WaitFinishDelayAsync();
    }

    private async Task<ShaderWarmupMaterialScanner.ShaderWarmupMaterialScanResult> CollectWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress,
        Func<bool> shouldStop
    )
    {
        progress.ShowScanning();
        WriteWarmupStatus("waiting-post-draw", "Waiting before shader resource scan");
        await WaitPostDrawAsync();

        var materials = await ShaderWarmupMaterialScanner.CollectAsync(tree, progress, shouldStop);
        PatchHelper.Log(Message.Collected(materials.Materials.Count));
        WriteWarmupStatus(
            "collected",
            $"Collected {materials.Materials.Count} unique shader warmup materials",
            materials.Diagnostics.ToEvidenceLines()
        );
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
