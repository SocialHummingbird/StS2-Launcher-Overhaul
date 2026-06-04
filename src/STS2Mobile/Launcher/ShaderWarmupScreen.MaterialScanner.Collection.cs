using System.Collections.Generic;
using System.Linq;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private sealed class WarmupMaterialCollection
        {
            private readonly Dictionary<string, Material> _materials = new();

            internal int Count => _materials.Count;

            internal bool Contains(string path)
                => _materials.ContainsKey(path);

            internal void SetMaterial(string path, Material material)
                => _materials[path] = material;

            internal void SetResource(string path, Resource resource)
            {
                if (TryCreateMaterial(resource, out var material))
                    SetMaterial(path, material);
            }

            internal void TryAddResource(string path, Resource resource)
            {
                if (TryCreateMaterial(resource, out var material))
                    _materials.TryAdd(path, material);
            }

            internal List<WarmupMaterial> UniqueByShader()
            {
                var unique = new Dictionary<string, WarmupMaterial>();
                foreach (var (path, mat) in _materials)
                {
                    var shaderKey = GetShaderKey(mat);
                    unique.TryAdd(shaderKey, WarmupMaterial.For(path, mat));
                }

                return unique.Values.ToList();
            }
        }
    }
}
