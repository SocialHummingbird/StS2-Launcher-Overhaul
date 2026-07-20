using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task ExtractSceneMaterialsAsync(
            string scenePath,
            WarmupMaterialCollection materials,
            SceneTree tree,
            LauncherMonotonicDeadline deadline,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            diagnostics.RecordThreadedLoadRequested();
            var result = await LauncherThreadedResourceLoader.LoadAsync(
                tree,
                scenePath,
                "PackedScene",
                deadline
            );
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
                diagnostics.RecordSceneExtractionFailure(
                    scenePath,
                    $"threaded load returned {result.Resource?.GetType().Name ?? "null"}"
                );
                return;
            }

            await ExtractMaterialsAsync(
                packed,
                scenePath,
                materials,
                tree,
                deadline,
                diagnostics
            );
        }

        private static async Task ExtractMaterialsAsync(
            PackedScene packed,
            string scenePath,
            WarmupMaterialCollection materials,
            SceneTree tree,
            LauncherMonotonicDeadline deadline,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
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

                    TryExtractMaterialProperty(
                        state,
                        scenePath,
                        nodeIndex,
                        propertyIndex,
                        propName,
                        materials,
                        diagnostics
                    );
                }

                if ((nodeIndex + 1) % 64 == 0
                    && !await LauncherAsyncYield.ProcessFrameAsync(tree, deadline))
                {
                    diagnostics.MarkDeadlineReached();
                    return;
                }
            }
        }

        private static bool IsMaterialProperty(string propName)
            => propName is "material" or "process_material" or "surface_material_override/0";

        private static void TryExtractMaterialProperty(
            SceneState state,
            string scenePath,
            int nodeIndex,
            int propertyIndex,
            string propName,
            WarmupMaterialCollection materials,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            try
            {
                var val = state.GetNodePropertyValue(nodeIndex, propertyIndex);
                if (val.Obj is Resource resource)
                    materials.TryAddResource(
                        $"{scenePath}#node{nodeIndex}#{propName}",
                        resource
                    );
            }
            catch (Exception ex)
            {
                diagnostics.RecordPropertyReadFailure(propName, scenePath, ex);
            }
        }
    }
}
