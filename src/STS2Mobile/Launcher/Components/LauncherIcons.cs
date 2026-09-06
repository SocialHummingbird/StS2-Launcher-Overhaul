using Godot;

namespace STS2Mobile.Launcher.Components;

// Small, consistently weighted interface symbols, rasterized at the device scale.
internal static class LauncherIcons
{
    internal static Texture2D Create(int destination, float scale)
    {
        var path = destination switch
        {
            0 => "<path d='m3 10 9-7 9 7v10H15v-7H9v7H3z'/>",
            1 => "<path d='M7 18H6a4 4 0 0 1-1-8 7 7 0 0 1 13-2 5 5 0 0 1 0 10h-1M12 12v9m-3-3 3 3 3-3'/>",
            2 => "<path d='m3 7 9-4 9 4-9 4zM3 12l9 4 9-4M3 17l9 4 9-4'/>",
            3 => "<rect x='3' y='3' width='7' height='7' rx='2'/><rect x='14' y='3' width='7' height='7' rx='2'/><rect x='3' y='14' width='7' height='7' rx='2'/><path d='M14 17.5h7m-3.5-3.5v7'/>",
            _ => "<circle cx='12' cy='12' r='9'/><path d='M9 9a3 3 0 0 1 6 0c0 2-3 2-3 5m0 3h.01'/>",
        };
        var image = new Image();
        image.LoadSvgFromString($"<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'><g fill='none' stroke='#ffffff' stroke-width='1.7' stroke-linecap='round' stroke-linejoin='round'>{path}</g></svg>", scale);
        return ImageTexture.CreateFromImage(image);
    }
}
