using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using STS2Mobile.Patches;

namespace LauncherUiPreview;

internal static class ModManagerCompatibilityTest
{
    internal static void Run(string directory)
    {
        var target = ModLoaderPatches.FindInitializeTarget(typeof(ModManager))
            ?? throw new Exception("The shipping manager has no supported initialization shape.");
        var inner = new ModManagerFileIo();
        var adapter = new LauncherModManagerFileIo(inner, Path.Combine(directory, "discovery-trigger"));
        var map = typeof(LauncherModManagerFileIo).GetInterfaceMap(typeof(IModManagerFileIo));
        Require(map.TargetMethods.Length == typeof(IModManagerFileIo).GetMethods().Length
            && map.TargetMethods.All(method => !method.IsAbstract && method.IsVirtual),
            "The adapter did not implement the actual shipping file-I/O interface.");
        var source = Path.Combine(directory, "forwarding-source.txt");
        File.WriteAllText(source, "native-file-io");
        Require(adapter.FileExists(source) && adapter.DirectoryExists(directory)
            && adapter.DirectoryExists(Path.Combine(directory, "discovery-trigger")),
            "Physical file access or the discovery entrypoint was lost.");
        Require(adapter.GetFilesAt(directory).SequenceEqual(inner.GetFilesAt(directory))
            && adapter.GetDirectoriesAt(directory).SequenceEqual(inner.GetDirectoriesAt(directory)),
            "The adapter projected or reordered physical file-system results.");
        using (var stream = adapter.OpenStream(source, Godot.FileAccess.ModeFlags.Read))
        using (var reader = new StreamReader(stream))
            Require(reader.ReadToEnd() == "native-file-io", "OpenStream did not forward the original request.");
        if (typeof(IModManagerFileIo).GetMethod("CopyFile") != null)
        {
            var destinationDirectory = Path.Combine(directory, "copy-target");
            adapter.MakeDirRecursive(destinationDirectory);
            var destination = Path.Combine(destinationDirectory, "copy.txt");
            Require(adapter.CopyFile(source, destination) == Error.Ok && File.ReadAllText(destination) == "native-file-io",
                "Current save-copy interface methods were not forwarded.");
        }
        GD.Print($"MOD_MANAGER_SHIPPING_CONTRACT initialize={target.ReturnType.Name} io={map.TargetMethods.Length}");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
