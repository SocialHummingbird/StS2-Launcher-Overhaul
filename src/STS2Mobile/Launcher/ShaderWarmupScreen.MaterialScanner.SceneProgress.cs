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
            int total
        )
        {
            if (index % 50 != 0)
                return;

            progress.SetDetail(Message.ScanningScenes(index, total));
            if (total > 0)
                progress.SetProgress((double)index / total * 50);
            if (tree != null)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
