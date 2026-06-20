using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private async Task WaitPostDrawAsync()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private async Task WaitFinishDelayAsync()
    {
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
    }
}
