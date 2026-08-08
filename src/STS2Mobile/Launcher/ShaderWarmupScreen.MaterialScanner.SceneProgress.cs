using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task ReportSceneScanProgressIfNeededAsync(
            SceneTree tree,
            ShaderWarmupProgress progress,
            int index,
            int total,
            LauncherMonotonicDeadline deadline
        )
        {
            if (index % 50 != 0)
                return;

            progress.ReportSceneScanProgress(index, total);
            await LauncherAsyncYield.ProcessFrameAsync(tree, deadline);
        }
    }
}
