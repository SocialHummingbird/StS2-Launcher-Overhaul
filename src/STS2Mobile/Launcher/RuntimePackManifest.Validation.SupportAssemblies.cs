using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private static string RuntimePackSupportAssemblyProblem(RuntimePackManifest manifest)
    {
        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { AndroidAssemblyFileName };
        var declaredSupportAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var supportAssembly in manifest.SupportAssemblies)
        {
            if (string.IsNullOrWhiteSpace(supportAssembly))
                return "runtime pack declares a blank support assembly";
            if (supportAssembly.IndexOfAny(new[] { '/', '\\' }) >= 0 || !string.Equals(System.IO.Path.GetFileName(supportAssembly), supportAssembly, StringComparison.Ordinal))
                return $"runtime pack support assembly has unsafe name: {supportAssembly}";
            if (!supportAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return $"runtime pack support assembly is not a DLL: {supportAssembly}";
            if (string.Equals(supportAssembly, AndroidAssemblyFileName, StringComparison.OrdinalIgnoreCase))
                return "runtime pack support assemblies must not redeclare sts2.dll";
            if (!declared.Add(supportAssembly))
                return $"runtime pack support assembly is duplicated: {supportAssembly}";
            declaredSupportAssemblies.Add(supportAssembly);

            var supportPath = System.IO.Path.Combine(manifest.DirectoryPath, supportAssembly);
            if (!File.Exists(supportPath))
                return $"runtime pack support assembly missing: {supportAssembly}";
            if (!manifest.SupportAssemblySha256.TryGetValue(supportAssembly, out var declaredSha256) || string.IsNullOrWhiteSpace(declaredSha256))
                return $"runtime pack support assembly hash missing: {supportAssembly}";
        }

        foreach (var supportHash in manifest.SupportAssemblySha256.Keys)
        {
            if (!declaredSupportAssemblies.Contains(supportHash))
                return $"runtime pack support assembly hash is undeclared: {supportHash}";
        }

        if (Directory.Exists(manifest.DirectoryPath))
        {
            foreach (var dll in Directory.EnumerateFiles(manifest.DirectoryPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var fileName = System.IO.Path.GetFileName(dll);
                if (!declared.Contains(fileName))
                    return $"runtime pack contains undeclared DLL: {fileName}";
            }
        }

        return string.Empty;
    }
}
