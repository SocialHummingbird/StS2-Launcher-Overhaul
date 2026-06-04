using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void TryLoadMaterialResource(
            string cleanName,
            string cleanPath,
            WarmupMaterialCollection materials
        )
        {
            try
            {
                if (!ResourceLoader.Exists(cleanPath))
                    return;

                if (cleanName.EndsWith(TresExtension))
                {
                    if (TryLoadTresMaterial(cleanPath, out var mat))
                        materials.SetMaterial(cleanPath, mat);
                    return;
                }

                var res = ResourceLoader.Load(cleanPath, null, ResourceLoader.CacheMode.Reuse);
                materials.SetResource(cleanPath, res);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(Message.ResourceLoadFailed(cleanPath, ex));
            }
        }

        private static bool TryLoadTresMaterial(string cleanPath, out Material material)
        {
            var resource = ResourceLoader.Load(
                cleanPath,
                "Material",
                ResourceLoader.CacheMode.Reuse
            );
            if (TryCreateMaterial(resource, out material))
                return true;

            resource = ResourceLoader.Load(
                cleanPath,
                "Shader",
                ResourceLoader.CacheMode.Reuse
            );
            return TryCreateMaterial(resource, out material);
        }
    }
}
