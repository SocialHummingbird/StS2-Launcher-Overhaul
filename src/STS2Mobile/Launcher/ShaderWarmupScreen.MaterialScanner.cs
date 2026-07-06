using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private const string GodotShaderExtension = ".gdshader";
        private const string MaterialExtension = ".material";
        private const string RemapExtension = ".remap";
        private const string ResourceRoot = "res://";
        private const string SceneExtension = ".tscn";
        private const string SceneRoot = "res://scenes";
        private const string TresExtension = ".tres";

        internal static async Task<ShaderWarmupMaterialScanResult> CollectAsync(
            SceneTree tree,
            ShaderWarmupProgress progress,
            Func<bool> shouldStop
        )
        {
            var materials = new WarmupMaterialCollection();
            var diagnostics = new ShaderWarmupMaterialScanDiagnostics();

            await ScanLooseMaterialsAsync(materials, tree, progress, diagnostics);
            if (!shouldStop())
                await ScanScenesAsync(materials, tree, progress, shouldStop, diagnostics);
            else
                diagnostics.SceneScanStoppedByBudget = true;

            var unique = materials.UniqueByShader();
            diagnostics.MaterialsBeforeDedup = materials.Count;
            diagnostics.UniqueMaterialCount = unique.Count;
            PatchHelper.Log(Message.UniqueShaders(materials.Count, unique.Count));
            diagnostics.LogSummary();
            return new ShaderWarmupMaterialScanResult(unique, diagnostics);
        }

        private static bool TryCreateMaterial(Resource resource, out Material material)
        {
            material = resource switch
            {
                Material resMat => resMat,
                Shader resShader => new ShaderMaterial
                {
                    Shader = resShader,
                },
                _ => null,
            };
            return material != null;
        }

        private static string GetShaderKey(Material mat)
        {
            if (mat is ShaderMaterial sm && sm.Shader != null)
                return sm.Shader.ResourcePath ?? sm.Shader.GetRid().ToString();
            if (mat is ParticleProcessMaterial)
                return $"particle#{mat.GetRid()}";
            return mat.ResourcePath ?? mat.GetRid().ToString();
        }

        private static string CleanResourceFileName(string fileName)
            => fileName.Replace(RemapExtension, "");

        private static string CleanResourcePath(string dirPath, string cleanName)
            => ChildPath(dirPath, cleanName);

        private static bool IsSceneFile(string cleanName)
            => cleanName.EndsWith(SceneExtension);

        private static bool IsSupportedMaterialFile(string cleanName)
            => cleanName.EndsWith(TresExtension)
                || cleanName.EndsWith(GodotShaderExtension)
                || cleanName.EndsWith(MaterialExtension);
    }
}
