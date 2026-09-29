using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private const string AssetLoadingSessionTypeName = "MegaCrit.Sts2.Core.Assets.AssetLoadingSession";

    // Android runs a publicized sts2.dll. The preload budget must still find its
    // targets there, or it silently leaves the unbounded title-screen burst in place.
    private static void AssetPreloadPatchTargetsSurvivePublicizer()
    {
        var referenceDirectory = FindGameReferenceDirectory();
        var tempDirectory = Path.Combine(Path.GetTempPath(), "sts2-publicizer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var publicizedPath = Path.Combine(tempDirectory, "sts2.dll");
            File.Copy(Path.Combine(referenceDirectory, "sts2.dll"), publicizedPath);
            var result = AndroidAssemblyPublicizer.Publicize(publicizedPath, referenceDirectory);
            True(result.Changed && result.FieldCount > 0, "The fixture must match what Android runs: a publicized sts2.dll.");

            RequireSupportedSessionIn(Path.Combine(referenceDirectory, "sts2.dll"), referenceDirectory);
            RequireSupportedSessionIn(publicizedPath, referenceDirectory);
        }
        finally
        {
            try { Directory.Delete(tempDirectory, recursive: true); } catch { }
        }
    }

    // Game and Godot assemblies stay in a collectible context so later tests keep
    // running without GodotSharp, which cannot initialize outside the engine.
    private static void RequireSupportedSessionIn(string gameAssemblyPath, string referenceDirectory)
    {
        var context = new GameAssemblyContext(gameAssemblyPath, referenceDirectory);
        try
        {
            var sessionType = context.LoadFromAssemblyName(new AssemblyName("sts2"))
                .GetType(AssetLoadingSessionTypeName, throwOnError: true)!;
            var require = context.LoadFromAssemblyName(new AssemblyName("STS2Mobile"))
                .GetType("STS2Mobile.Patches.AndroidAssetPreloadPatches", throwOnError: true)!
                .GetMethod("RequireSupportedSession", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                require.Invoke(null, new object[] { sessionType });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }
        finally
        {
            context.Unload();
        }
    }

    // The launcher project references the game assemblies without copying them.
    private static string FindGameReferenceDirectory()
    {
        var relative = Path.Combine("upstream", "godot-export", ".godot", "mono", "publish", "arm64");
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, relative);
            if (File.Exists(Path.Combine(candidate, "sts2.dll")))
                return candidate;
        }

        throw new FileNotFoundException($"Game reference assemblies were not found under {relative}.");
    }

    private sealed class GameAssemblyContext : AssemblyLoadContext
    {
        private readonly string _gameAssemblyPath;
        private readonly string _referenceDirectory;

        internal GameAssemblyContext(string gameAssemblyPath, string referenceDirectory)
            : base("asset-preload-targets", isCollectible: true)
        {
            _gameAssemblyPath = gameAssemblyPath;
            _referenceDirectory = referenceDirectory;
        }

        // The launcher and its packages come from the test output and game
        // dependencies from the reference directory; the runtime stays shared.
        protected override Assembly? Load(AssemblyName name)
        {
            if (name.Name == "sts2")
                return LoadFromAssemblyPath(_gameAssemblyPath);
            if (name.Name!.StartsWith("System", StringComparison.Ordinal)
                || name.Name.StartsWith("Microsoft.", StringComparison.Ordinal)
                || name.Name is "mscorlib" or "netstandard")
                return null;

            foreach (var directory in new[] { AppContext.BaseDirectory, _referenceDirectory })
            {
                var path = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(path))
                    return LoadFromAssemblyPath(path);
            }

            return null;
        }
    }
}
