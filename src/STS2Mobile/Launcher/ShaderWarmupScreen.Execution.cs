using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private async Task RunWarmupAsync(LauncherMonotonicDeadline deadline)
    {
        bool previousRenderCrashSuspected = _previousRenderCrashSuspected;
        var warmup = CreateWarmupRun(deadline);
        WriteWarmupStatus("collecting", "Collecting shader warmup materials");

        var scan = await CollectWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress,
            warmup.Deadline
        );
        var materials = scan.Materials;

        if (warmup.IsOverBudget)
        {
            warmup.CompletePartialAndReport(0, materials.Count);
            MarkWarmupComplete();
            WriteWarmupStatus(
                "completed-deadline",
                $"Shader warmup reached its {WarmupTimeBudgetSeconds}s deadline after collecting {materials.Count} materials; continuing startup",
                MergeEvidence(
                    new[]
                    {
                        $"Elapsed ms: {warmup.ElapsedMilliseconds}",
                        "Classification: hard deadline reached during resource scan",
                    },
                    scan.Diagnostics.ToEvidenceLines()
                )
            );
            PatchHelper.Log(Message.TimeBudgetReached(WarmupTimeBudgetSeconds));
            return;
        }

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

        var renderPlan = ShaderWarmupRenderPlan.ForMaterialCount(
            materials.Count,
            previousRenderCrashSuspected
        );
        WriteWarmupStatus(
            "rendering",
            $"Rendering {renderPlan.TargetMaterialCount} of {materials.Count} shader warmup materials using plan {renderPlan.Name}",
            renderPlan.ToEvidenceLines()
        );
        int rendered = await RenderWarmupMaterialsAsync(
            warmup.Tree,
            warmup.Progress,
            materials,
            renderPlan,
            warmup.Deadline
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
                        $"Classification: {renderPlan.CompletionClassification()}",
                    },
                    renderPlan.ToEvidenceLines(),
                    scan.Diagnostics.ToEvidenceLines()
                )
            );
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
                    $"Classification: {renderPlan.CompletionClassification()}",
                },
                renderPlan.ToEvidenceLines(),
                scan.Diagnostics.ToEvidenceLines()
            )
        );
    }

    private async Task<ShaderWarmupMaterialScanner.ShaderWarmupMaterialScanResult> CollectWarmupMaterialsAsync(
        SceneTree tree,
        ShaderWarmupProgress progress,
        LauncherMonotonicDeadline deadline
    )
    {
        progress.ShowScanning();
        WriteWarmupStatus("waiting-post-draw", "Waiting before shader resource scan");
        await WaitPostDrawAsync(deadline);

        var materials = await ShaderWarmupMaterialScanner.CollectAsync(tree, progress, deadline);
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
        ShaderWarmupRenderPlan renderPlan,
        LauncherMonotonicDeadline deadline
    )
    {
        progress.ShowCompiling();
        var renderer = ShaderWarmupRenderer.ForScreen(this, tree, progress, renderPlan, deadline);
        return await renderer.RenderAsync(materials);
    }

    private static void MarkWarmupComplete()
        => WriteWarmupVersion();
}
