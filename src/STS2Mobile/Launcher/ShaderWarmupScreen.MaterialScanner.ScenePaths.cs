using System.Collections.Generic;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void CollectScenePaths(string dirPath, List<string> paths)
        {
            VisitFiles(dirPath, (currentDir, fileName) =>
            {
                var cleanName = fileName.Replace(RemapExtension, "");
                if (!cleanName.EndsWith(SceneExtension))
                    return;

                var cleanPath = ChildPath(currentDir, cleanName);
                if (ResourceLoader.Exists(cleanPath))
                    paths.Add(cleanPath);
            });
        }
    }
}
