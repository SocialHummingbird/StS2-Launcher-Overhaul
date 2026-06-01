using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private void BuildUI(Vector2 vpSize)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var scale = Math.Max(vpSize.X, vpSize.Y) / 960f;

        var bg = new ScreenBackground();
        AddChild(bg);

        var panel = new StyledPanel(scale, widthRatio: 0.5f);
        panel.UpdateSizeFromViewport(vpSize);
        AddChild(panel);

        _statusLabel = new StyledLabel("Compiling shaders...", scale, fontSize: 20);
        panel.AddContent(_statusLabel);

        _progressBar = new StyledProgressBar(scale);
        _progressBar.MinValue = 0;
        _progressBar.MaxValue = 100;
        _progressBar.Value = 0;
        _progressBar.ShowPercentage = true;
        panel.AddContent(_progressBar);

        _detailLabel = new StyledLabel("Enumerating resources...", scale, fontSize: 12);
        _detailLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
        panel.AddContent(_detailLabel);
    }
}
