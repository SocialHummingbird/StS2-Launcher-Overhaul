using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class AndroidAssemblyPublicizer
{
    internal static Result Publicize(string assemblyPath, params string[] additionalProbePaths)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            return Result.NotChanged("assembly missing");

        var resolver = new DefaultAssemblyResolver();
        var searchDirectories = BuildResolverSearchDirectories(assemblyPath, additionalProbePaths);
        foreach (var directory in searchDirectories)
            resolver.AddSearchDirectory(directory);

        using var input = new MemoryStream(File.ReadAllBytes(assemblyPath));
        using var module = ModuleDefinition.ReadModule(
            input,
            new ReaderParameters
            {
                AssemblyResolver = resolver,
                InMemory = true,
                ReadingMode = ReadingMode.Immediate,
            }
        );

        var typeCount = 0;
        var methodCount = 0;
        var fieldCount = 0;
        foreach (var type in WalkTypes(module.Types))
        {
            if (PublicizeType(type))
                typeCount++;

            foreach (var method in type.Methods)
            {
                if (PublicizeMethod(method))
                    methodCount++;
            }

            foreach (var field in type.Fields)
            {
                if (PublicizeField(field))
                    fieldCount++;
            }
        }

        if (typeCount == 0 && methodCount == 0 && fieldCount == 0)
            return Result.NotChanged("already public");

        WriteModuleOverOriginal(module, assemblyPath);
        var result = new Result(true, typeCount, methodCount, fieldCount, "publicized");
        PatchHelper.Log(
            $"[Launcher] Android Harmony visibility publicizer updated {Path.GetFileName(assemblyPath)}: types={typeCount} methods={methodCount} fields={fieldCount}"
        );
        return result;
    }

    private static string[] BuildResolverSearchDirectories(string assemblyPath, IEnumerable<string> additionalProbePaths)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddDirectoryForPath(directories, assemblyPath);

        if (additionalProbePaths != null)
        {
            foreach (var path in additionalProbePaths)
                AddDirectoryForPath(directories, path);
        }

        AddDirectoryForPath(directories, AppContext.BaseDirectory);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                AddDirectoryForPath(directories, assembly.Location);
            }
            catch
            {
            }
        }

        return directories.ToArray();
    }

    private static void AddDirectoryForPath(HashSet<string> directories, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Directory.Exists(fullPath)
                ? fullPath
                : Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                directories.Add(directory);
        }
        catch
        {
        }
    }

    private static IEnumerable<TypeDefinition> WalkTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in WalkTypes(type.NestedTypes))
                yield return nested;
        }
    }

    private static bool PublicizeType(TypeDefinition type)
    {
        if (type == null)
            return false;

        var before = type.Attributes;
        if (type.IsNested)
        {
            type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
        }
        else if (!type.IsPublic)
        {
            type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.Public;
        }

        return type.Attributes != before;
    }

    private static bool PublicizeMethod(MethodDefinition method)
    {
        if (method == null)
            return false;

        if (method.IsConstructor && method.IsStatic)
            return false;

        var before = method.Attributes;
        method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
        return method.Attributes != before;
    }

    private static bool PublicizeField(FieldDefinition field)
    {
        if (field == null)
            return false;

        var before = field.Attributes;
        field.Attributes = (field.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
        return field.Attributes != before;
    }

    private static void WriteModuleOverOriginal(ModuleDefinition module, string path)
    {
        var tempPath = path + ".publicized-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            module.Write(tempPath);
            File.Copy(tempPath, path, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
            }
        }
    }

    internal readonly record struct Result(
        bool Changed,
        int TypeCount,
        int MethodCount,
        int FieldCount,
        string Status
    )
    {
        internal static Result NotChanged(string status) => new(false, 0, 0, 0, status);
    }
}
