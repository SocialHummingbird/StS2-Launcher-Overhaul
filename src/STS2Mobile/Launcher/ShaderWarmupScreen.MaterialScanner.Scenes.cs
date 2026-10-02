using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;
internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task ScanScenesAsync(WarmupMaterialCollection materials, SceneTree tree, ShaderWarmupProgress progress, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            var scenePaths = new List<string>();
            CollectScenePaths(SceneRoot, scenePaths, deadline, diagnostics);
            diagnostics.SceneCount = scenePaths.Count;
            PatchHelper.Log(Message.FoundScenes(scenePaths.Count));
            for (int i = 0; i < scenePaths.Count; i++)
            {
                if (deadline.IsExpired)
                {
                    PatchHelper.Log(Message.SceneScanStoppedByBudget(i, scenePaths.Count));
                    diagnostics.ScannedSceneCount = i;
                    diagnostics.SceneScanStoppedByBudget = true;
                    diagnostics.MarkDeadlineReached();
                    return;
                }

                await ExtractSceneMaterialsAsync(scenePaths[i], materials, tree, deadline, diagnostics);
                if (deadline.IsExpired)
                {
                    diagnostics.ScannedSceneCount = i;
                    diagnostics.SceneScanStoppedByBudget = true;
                    diagnostics.MarkDeadlineReached();
                    return;
                }

                diagnostics.ScannedSceneCount = i + 1;
                await ReportSceneScanProgressIfNeededAsync(tree, progress, i, scenePaths.Count, deadline);
            }
        }

        private static async Task ExtractSceneMaterialsAsync(string scenePath, WarmupMaterialCollection materials, SceneTree tree, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            diagnostics.RecordThreadedLoadRequested();
            var result = await LauncherThreadedResourceLoader.LoadAsync(tree, scenePath, "PackedScene", deadline);
            if (result.Outcome == LauncherThreadedLoadOutcome.DeadlineExceeded)
            {
                diagnostics.RecordThreadedLoadTimedOut();
                diagnostics.MarkDeadlineReached();
                return;
            }

            if (result.Outcome == LauncherThreadedLoadOutcome.Failed)
            {
                diagnostics.RecordSceneExtractionFailure(scenePath, result.Failure);
                return;
            }

            diagnostics.RecordThreadedLoadCompleted();
            if (result.Resource is not PackedScene packed)
            {
                diagnostics.RecordSceneExtractionFailure(scenePath, $"threaded load returned {result.Resource?.GetType().Name ?? "null"}");
                return;
            }

            await ExtractMaterialsAsync(packed, scenePath, materials, tree, deadline, diagnostics);
        }

        private static async Task ExtractMaterialsAsync(PackedScene packed, string scenePath, WarmupMaterialCollection materials, SceneTree tree, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            var state = packed.GetState();
            int nodeCount = state.GetNodeCount();
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
                if (deadline.IsExpired)
                {
                    diagnostics.MarkDeadlineReached();
                    return;
                }

                int propertyCount = state.GetNodePropertyCount(nodeIndex);
                for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
                {
                    var propName = state.GetNodePropertyName(nodeIndex, propertyIndex).ToString();
                    if (!IsMaterialProperty(propName))
                        continue;
                    TryExtractMaterialProperty(state, scenePath, nodeIndex, propertyIndex, propName, materials, diagnostics);
                }

                if ((nodeIndex + 1) % 64 == 0 && !await LauncherAsyncYield.ProcessFrameAsync(tree, deadline))
                {
                    diagnostics.MarkDeadlineReached();
                    return;
                }
            }
        }

        private static bool IsMaterialProperty(string propName) => propName is "material" or "process_material" or "surface_material_override/0";
        private static void TryExtractMaterialProperty(SceneState state, string scenePath, int nodeIndex, int propertyIndex, string propName, WarmupMaterialCollection materials, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            try
            {
                var val = state.GetNodePropertyValue(nodeIndex, propertyIndex);
                if (val.Obj is Resource resource)
                    materials.TryAddResource($"{scenePath}#node{nodeIndex}#{propName}", resource);
            }
            catch (Exception ex)
            {
                diagnostics.RecordPropertyReadFailure(propName, scenePath, ex);
            }
        }

        private static void CollectScenePaths(string dirPath, List<string> paths, LauncherMonotonicDeadline deadline, ShaderWarmupMaterialScanDiagnostics diagnostics)
        {
            VisitFiles(dirPath, (currentDir, fileName) =>
            {
                var cleanName = CleanResourceFileName(fileName);
                if (!IsSceneFile(cleanName))
                    return;
                var cleanPath = CleanResourcePath(currentDir, cleanName);
                if (ShouldSkipScenePathForWarmup(cleanPath))
                    return;
                paths.Add(cleanPath);
            }, deadline, diagnostics);
        }

        private static bool ShouldSkipScenePathForWarmup(string scenePath) => scenePath.Contains("/daily_run/", StringComparison.OrdinalIgnoreCase);
        private static async Task ReportSceneScanProgressIfNeededAsync(SceneTree tree, ShaderWarmupProgress progress, int index, int total, LauncherMonotonicDeadline deadline)
        {
            if (index % 50 != 0)
                return;
            progress.ReportSceneScanProgress(index, total);
            await LauncherAsyncYield.ProcessFrameAsync(tree, deadline);
        }
    }
}
