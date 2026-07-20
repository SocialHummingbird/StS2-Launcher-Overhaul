using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed class ShaderWarmupPresentationHost
{
    private const string AndroidLayerName = "STS2MobileShaderWarmup";

    private readonly ShaderWarmupPresentationLifecycle _lifecycle = new();
    private readonly Node _root;
    private readonly ShaderWarmupScreen _screen;

    private ShaderWarmupPresentationHost(Node root, ShaderWarmupScreen screen)
    {
        _root = root;
        _screen = screen;
    }

    internal static ShaderWarmupPresentationHost Show(Node parent)
    {
        var screen = new ShaderWarmupScreen();
        Node root = screen;
        if (OperatingSystem.IsAndroid())
        {
            var layer = new CanvasLayer
            {
                Name = AndroidLayerName,
                Layer = StartupPresentationLayerPolicy.ShaderWarmupCanvasLayer,
            };
            layer.AddChild(screen);
            root = layer;
        }

        parent.AddChild(root);
        var presentation = new ShaderWarmupPresentationHost(root, screen);
        presentation._lifecycle.MarkVisible();
        PatchHelper.Log(
            OperatingSystem.IsAndroid()
                ? $"Shader warmup presentation visible above startup cover; canvasLayer={StartupPresentationLayerPolicy.ShaderWarmupCanvasLayer}"
                : "Shader warmup presentation visible in the startup canvas"
        );
        return presentation;
    }

    internal async Task RunAsync()
        => await _screen.RunAsync();

    internal bool QueueFree()
    {
        if (!_lifecycle.TryBeginCleanup())
            return true;

        try
        {
            HideRoot();
            _root.QueueFree();
            PatchHelper.Log(
                "Shader warmup presentation cleanup requested; startup cover remains active"
            );
            return true;
        }
        catch (ObjectDisposedException)
        {
            PatchHelper.Log("Shader warmup presentation was already disposed");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Shader warmup presentation cleanup failed: {ex.Message}");
            return false;
        }
    }

    private void HideRoot()
    {
        _root.ProcessMode = Node.ProcessModeEnum.Disabled;
        if (_root is CanvasLayer canvasLayer)
            canvasLayer.Visible = false;
        else if (_root is CanvasItem canvasItem)
            canvasItem.Visible = false;
    }
}
