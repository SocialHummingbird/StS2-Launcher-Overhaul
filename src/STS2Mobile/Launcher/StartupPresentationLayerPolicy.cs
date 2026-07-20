namespace STS2Mobile.Launcher;

internal static class StartupPresentationLayerPolicy
{
    internal const int StartupStatusCanvasLayer = 0;
    internal const int StartupStatusZIndex = 4096;
    internal const int ShaderWarmupCanvasLayer = 127;
    internal const int RecoveryCanvasLayer = 128;

    internal static bool HasValidOrdering
        => ShaderWarmupCanvasLayer > StartupStatusCanvasLayer
            && RecoveryCanvasLayer > ShaderWarmupCanvasLayer;
}
