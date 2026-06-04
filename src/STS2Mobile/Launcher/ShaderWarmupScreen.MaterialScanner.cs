using System.Collections.Generic;
using System.Linq;
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

        internal static async Task<List<WarmupMaterial>> CollectAsync(
            SceneTree tree,
            ShaderWarmupProgress progress
        )
        {
            var materials = new Dictionary<string, Material>();

            await ScanLooseMaterialsAsync(materials, tree, progress);
            await ScanScenesAsync(materials, tree, progress);

            var unique = UniqueByShader(materials);
            PatchHelper.Log(Message.UniqueShaders(materials.Count, unique.Count));
            return unique;
        }

        private static List<WarmupMaterial> UniqueByShader(
            Dictionary<string, Material> materials
        )
        {
            var unique = new Dictionary<string, WarmupMaterial>();
            foreach (var (path, mat) in materials)
            {
                var shaderKey = GetShaderKey(mat);
                unique.TryAdd(shaderKey, WarmupMaterial.For(path, mat));
            }

            return unique.Values.ToList();
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
