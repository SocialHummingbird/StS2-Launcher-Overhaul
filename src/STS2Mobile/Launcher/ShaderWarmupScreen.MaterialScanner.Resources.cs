using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void CollectLooseMaterials(
            string dirPath,
            WarmupMaterialCollection materials
        )
        {
            VisitFiles(dirPath, (currentDir, fileName) =>
                TryCollectMaterialFile(currentDir, fileName, materials));
        }

        private static void TryCollectMaterialFile(
            string dirPath,
            string fileName,
            WarmupMaterialCollection materials
        )
        {
            var cleanName = CleanResourceFileName(fileName);
            if (!IsSupportedMaterialFile(cleanName))
                return;

            var cleanPath = CleanResourcePath(dirPath, cleanName);
            if (materials.ContainsKey(cleanPath))
                return;

            TryLoadMaterialResource(cleanName, cleanPath, materials);
        }
    }
}
