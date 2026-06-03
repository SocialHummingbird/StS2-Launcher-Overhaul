using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static async Task ScanLooseMaterialsAsync(
            Dictionary<string, Material> materials,
            SceneTree tree,
            ShaderWarmupProgress progress
        )
        {
            CollectLooseMaterials(ResourceRoot, materials);
            PatchHelper.Log(Message.FoundLooseMaterials(materials.Count));
            progress.ShowMaterialsFound(materials.Count);
            if (tree != null)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
