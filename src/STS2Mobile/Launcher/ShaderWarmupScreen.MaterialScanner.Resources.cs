using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void CollectLooseMaterials(
            string dirPath,
            WarmupMaterialCollection materials,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            VisitFiles(dirPath, (currentDir, fileName) =>
                TryCollectMaterialFile(currentDir, fileName, materials, diagnostics), diagnostics);
        }

        private static void TryCollectMaterialFile(
            string dirPath,
            string fileName,
            WarmupMaterialCollection materials,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            var cleanName = CleanResourceFileName(fileName);
            if (!IsSupportedMaterialFile(cleanName))
                return;

            var cleanPath = CleanResourcePath(dirPath, cleanName);
            if (materials.Contains(cleanPath))
                return;

            TryLoadMaterialResource(cleanName, cleanPath, materials, diagnostics);
        }
    }
}
