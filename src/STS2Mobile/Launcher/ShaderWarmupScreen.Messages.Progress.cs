namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class Message
    {
        internal static string Compiled(int materialCount, long elapsedMilliseconds)
            => $"Compiled {materialCount} shaders in {elapsedMilliseconds}ms";

        internal static string FoundMaterialsDetail(int materialCount)
            => $"Found {materialCount} materials...";

        internal static string ScanningScenes(int index, int total)
            => $"Scanning scenes... {index} / {total}";

        internal static string CompilingProgress(int completed, int total)
            => $"Compiling {completed} / {total}";
    }
}
