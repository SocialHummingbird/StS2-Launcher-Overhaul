using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed partial class ScreenBackground : Control
{
    internal ScreenBackground()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Draw()
    {
        var size = GetRect().Size;
        DrawRect(new Rect2(Vector2.Zero, size), LauncherComponentTheme.ScreenBackground);

        DrawCircle(
            new Vector2(size.X * 0.18f, size.Y * 0.18f),
            Mathf.Max(size.X, size.Y) * 0.34f,
            LauncherComponentTheme.BrandGlowCyan
        );
        DrawCircle(
            new Vector2(size.X * 0.84f, size.Y * 0.88f),
            Mathf.Max(size.X, size.Y) * 0.38f,
            LauncherComponentTheme.BrandGlowOrange
        );

        var stripeWidth = Mathf.Max(2f, size.X * 0.006f);
        DrawRect(
            new Rect2(size.X * 0.08f, 0, stripeWidth, size.Y),
            new Color(0.03f, 0.42f, 0.52f, 0.22f)
        );
        DrawRect(
            new Rect2(size.X * 0.105f, 0, stripeWidth * 0.55f, size.Y),
            new Color(1.0f, 0.48f, 0.02f, 0.18f)
        );
        DrawRect(
            new Rect2(0, size.Y * 0.86f, size.X, Mathf.Max(2f, size.Y * 0.006f)),
            new Color(1.0f, 0.48f, 0.02f, 0.16f)
        );
    }
}
