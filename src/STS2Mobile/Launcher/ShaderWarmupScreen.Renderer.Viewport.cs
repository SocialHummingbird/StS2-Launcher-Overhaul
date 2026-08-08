using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private sealed partial class ShaderWarmupRenderer
    {
        private static SubViewport CreateViewport()
            => new()
            {
                Size = new Vector2I(ViewportWidth, ViewportHeight),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                TransparentBg = true,
            };

        private static ImageTexture CreateWhiteTexture()
        {
            var whiteImage = Image.CreateEmpty(
                TextureWidth,
                TextureHeight,
                false,
                Image.Format.Rgba8
            );
            whiteImage.SetPixel(0, 0, Colors.White);
            return ImageTexture.CreateFromImage(whiteImage);
        }
    }
}
