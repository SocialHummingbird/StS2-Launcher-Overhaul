using System;

namespace STS2Mobile.Launcher;

internal enum AndroidMainMenuResourceKind
{
    Texture,
    Shader,
    Material,
}

internal readonly struct AndroidMainMenuResource
{
    internal AndroidMainMenuResource(
        string logicalPath,
        AndroidMainMenuResourceKind kind
    )
    {
        LogicalPath = logicalPath;
        Kind = kind;
    }

    internal string LogicalPath { get; }
    internal AndroidMainMenuResourceKind Kind { get; }
}

internal static class AndroidMainMenuWorkingSet
{
    internal const int MaximumResourceCount = 32;

    // These resources repeatedly loaded during the first five seconds after
    // NMainMenu across public and public-beta hardware captures.
    internal static readonly AndroidMainMenuResource[] Resources =
    [
        Texture("res://images/atlases/intent_atlas.png"),
        Texture("res://images/ui/reward_screen/reward_panel.png"),
        Texture("res://images/ui/run_history/monster.png"),
        Texture("res://images/ui/run_history/elite.png"),
        Texture("res://images/ui/run_history/event.png"),
        Texture("res://images/ui/run_history/shop.png"),
        Texture("res://images/ui/run_history/treasure.png"),
        Texture("res://images/ui/run_history/rest_site.png"),
        Texture("res://images/ui/run_history/unknown_monster.png"),
        Texture("res://images/ui/run_history/unknown_elite.png"),
        Texture("res://images/ui/run_history/unknown_shop.png"),
        Texture("res://images/ui/run_history/unknown_treasure.png"),
        Texture("res://images/packed/timeline/epoch_slot_locked.png"),
        Texture("res://images/packed/timeline/epoch_slot_locked_small.png"),
        Material("res://materials/boss_map_point_unavailable.tres"),
        Shader("res://shaders/vfx/boss_map_point_unavailable.gdshader"),
        Shader("res://shaders/button_pulse.gdshader"),
        Shader("res://scenes/combat/doom_bar.gdshader"),
        Shader("res://shaders/power_flash_vfx.gdshader"),
        Shader("res://shaders/relic.gdshader"),
        Shader("res://shaders/power.gdshader"),
        Shader("res://scenes/ui/normal_map_point.gdshader"),
        Shader("res://shaders/blur/Blur.gdshader"),
        Shader("res://shaders/blur/canvas_group_mask_blur.gdshader"),
        Shader("res://shaders/boss_map_point.gdshader"),
        Shader("res://shaders/map_drawing/line_draw.gdshader"),
        Shader("res://shaders/map_drawing/line_erase.gdshader"),
        Shader("res://shaders/vfx/ui/vfx_ui_epoch_unlock_chains_shader.gdshader"),
    ];

    private static AndroidMainMenuResource Texture(string path)
        => new(path, AndroidMainMenuResourceKind.Texture);

    private static AndroidMainMenuResource Shader(string path)
        => new(path, AndroidMainMenuResourceKind.Shader);

    private static AndroidMainMenuResource Material(string path)
        => new(path, AndroidMainMenuResourceKind.Material);
}
