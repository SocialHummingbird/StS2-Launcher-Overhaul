using System;
using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void ExtractSceneMaterials(
            string scenePath,
            Dictionary<string, Material> materials
        )
        {
            try
            {
                var packed = ResourceLoader.Load<PackedScene>(
                    scenePath,
                    null,
                    ResourceLoader.CacheMode.Reuse
                );
                if (packed != null)
                    ExtractMaterials(packed, scenePath, materials);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.SceneExtractFailed(scenePath, ex));
            }
        }

        private static void ExtractMaterials(
            PackedScene packed,
            string scenePath,
            Dictionary<string, Material> materials
        )
        {
            var state = packed.GetState();
            int nodeCount = state.GetNodeCount();
            for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
            {
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
                        materials
                    );
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
            Dictionary<string, Material> materials
        )
        {
            try
            {
                var val = state.GetNodePropertyValue(nodeIndex, propertyIndex);
                if (val.Obj is Resource resource && TryCreateMaterial(resource, out var material))
                    materials.TryAdd($"{scenePath}#node{nodeIndex}#{propName}", material);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.PropertyReadFailed(propName, scenePath, ex));
            }
        }
    }
}
