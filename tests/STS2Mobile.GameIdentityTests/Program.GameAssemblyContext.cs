using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    // Loads the launcher and one sts2.dll into a collectible context so patch
    // targets can be checked without GodotSharp, which cannot initialize outside
    // the engine, entering the default context used by the other tests.
    private static T RunInGameAssemblyContext<T>(string gameAssemblyPath, Func<Assembly, Assembly, T> run)
    {
        var context = new GameAssemblyContext(gameAssemblyPath, FindGameReferenceDirectory());
        try
        {
            var game = context.LoadFromAssemblyName(new AssemblyName("sts2"));
            var launcher = context.LoadFromAssemblyName(new AssemblyName("STS2Mobile"));
            try
            {
                return run(game, launcher);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
        finally
        {
            context.Unload();
        }
    }

    private static object? InvokeLauncherStatic(Assembly launcher, string typeName, string methodName, params object[] args)
        => launcher.GetType(typeName, throwOnError: true)!
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args);

    private static string ReferenceGameAssemblyPath()
        => Path.Combine(FindGameReferenceDirectory(), "sts2.dll");

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

    // A temporary copy of the reference sts2.dll rewritten by the launcher's
    // Android publicizer, matching what the runtime pack runs on device.
    private sealed class PublicizedGameAssembly : IDisposable
    {
        private readonly string _directory =
            Path.Combine(Path.GetTempPath(), "sts2-publicizer-" + Guid.NewGuid().ToString("N"));

        internal PublicizedGameAssembly()
        {
            Directory.CreateDirectory(_directory);
            AssemblyPath = Path.Combine(_directory, "sts2.dll");
            File.Copy(ReferenceGameAssemblyPath(), AssemblyPath);
            var result = AndroidAssemblyPublicizer.Publicize(AssemblyPath, FindGameReferenceDirectory());
            True(result.Changed && result.FieldCount > 0, "The fixture must match what Android runs: a publicized sts2.dll.");
        }

        internal string AssemblyPath { get; }

        public void Dispose()
        {
            try { Directory.Delete(_directory, recursive: true); } catch { }
        }
    }

    private sealed class GameAssemblyContext : AssemblyLoadContext
    {
        private readonly string _gameAssemblyPath;
        private readonly string _referenceDirectory;

        internal GameAssemblyContext(string gameAssemblyPath, string referenceDirectory)
            : base("game-assembly-targets", isCollectible: true)
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
