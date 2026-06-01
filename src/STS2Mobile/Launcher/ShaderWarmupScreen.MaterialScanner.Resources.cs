using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void CollectLooseMaterials(string dirPath, Dictionary<string, Material> materials)
        {
            VisitFiles(dirPath, (currentDir, fileName) =>
                TryCollectMaterialFile(currentDir, fileName, materials));
        }

        private static void TryCollectMaterialFile(
            string dirPath,
            string fileName,
            Dictionary<string, Material> materials
        )
        {
            var cleanName = fileName.Replace(RemapExtension, "");
            if (!IsSupportedMaterialFile(cleanName))
                return;

            var cleanPath = ChildPath(dirPath, cleanName);
            if (materials.ContainsKey(cleanPath))
                return;

            TryLoadMaterialResource(cleanName, cleanPath, materials);
        }

        private static bool IsSupportedMaterialFile(string cleanName)
            => cleanName.EndsWith(TresExtension)
                || cleanName.EndsWith(GodotShaderExtension)
                || cleanName.EndsWith(MaterialExtension);
    }
}
