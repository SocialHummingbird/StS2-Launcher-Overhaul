using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task ScanScenesAsync(
            WarmupMaterialCollection materials,
            SceneTree tree,
            ShaderWarmupProgress progress,
            Func<bool> shouldStop,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            var scenePaths = new List<string>();
            CollectScenePaths(SceneRoot, scenePaths, diagnostics);
            diagnostics.SceneCount = scenePaths.Count;
            PatchHelper.Log(Message.FoundScenes(scenePaths.Count));

            for (int i = 0; i < scenePaths.Count; i++)
            {
                if (shouldStop())
                {
                    PatchHelper.Log(Message.SceneScanStoppedByBudget(i, scenePaths.Count));
                    diagnostics.ScannedSceneCount = i;
                    diagnostics.SceneScanStoppedByBudget = true;
                    return;
                }

                ExtractSceneMaterials(scenePaths[i], materials, diagnostics);
                diagnostics.ScannedSceneCount = i + 1;
                await ReportSceneScanProgressIfNeededAsync(tree, progress, i, scenePaths.Count);
            }
        }
    }
}
