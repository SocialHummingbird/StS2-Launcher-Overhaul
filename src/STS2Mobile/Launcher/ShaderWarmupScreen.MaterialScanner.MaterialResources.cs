using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task<LauncherThreadedLoadResult> LoadMaterialResourceAsync(
            SceneTree tree,
            string path,
            LauncherMonotonicDeadline deadline
        )
        {
            return await LauncherThreadedResourceLoader.LoadAsync(
                tree,
                path,
                string.Empty,
                deadline
            );
        }
    }
}
