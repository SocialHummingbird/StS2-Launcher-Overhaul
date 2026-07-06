using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task ScanLooseMaterialsAsync(
            WarmupMaterialCollection materials,
            SceneTree tree,
            ShaderWarmupProgress progress,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            CollectLooseMaterials(ResourceRoot, materials, diagnostics);
            PatchHelper.Log(Message.FoundLooseMaterials(materials.Count));
            progress.ShowMaterialsFound(materials.Count);
            if (tree != null)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
