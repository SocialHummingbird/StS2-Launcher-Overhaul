using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            internal async Task<List<WarmupMaterial>> UniqueByShaderAsync(
                SceneTree tree,
                LauncherMonotonicDeadline deadline,
                ShaderWarmupMaterialScanDiagnostics diagnostics
            )
            {
                var unique = new Dictionary<string, WarmupMaterial>();
                int processed = 0;
                foreach (var (path, mat) in _materials)
                {
                    if (deadline.IsExpired)
                    {
                        diagnostics.MarkDeduplicationStoppedByBudget(processed);
                        break;
                    }

                    var shaderKey = GetShaderKey(mat);
                    unique.TryAdd(shaderKey, WarmupMaterial.For(path, mat));
                    processed++;

                    if (processed % 32 == 0
                        && !await LauncherAsyncYield.ProcessFrameAsync(tree, deadline))
                    {
                        diagnostics.MarkDeduplicationStoppedByBudget(processed);
                        break;
                    }
                }

                diagnostics.DeduplicatedMaterialCount = processed;
                return unique.Values.ToList();
            }
        }
    }
}
