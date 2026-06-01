using System;
using System.IO;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const string FmodCreditText = "Made using FMOD Studio by Firelight Technologies Pty Ltd.";
    private const int FmodCreditFontSize = 8;
    private const string FmodLogoFileName = "fmod_logo.png";
    private const int FmodLogoHeight = 30;
    private const int FmodLogoWidth = 120;
    private static readonly Color FmodCreditColor = new(0.5f, 0.5f, 0.55f);

    private static VBoxContainer BuildFmodAttributionSection(float scale)
    {
        var section = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.End,
        };

        var logo = LoadFmodLogo(scale);
        if (logo != null)
            section.AddChild(logo);

        var credit = new StyledLabel(
            FmodCreditText,
            scale,
            fontSize: FmodCreditFontSize
        );
        credit.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            FmodCreditColor
        );
        section.AddChild(credit);
        return section;
    }

    private static TextureRect LoadFmodLogo(float scale)
    {
        try
        {
            var logoPath = Path.Combine(OS.GetDataDir(), FmodLogoFileName);
            if (!File.Exists(logoPath))
            {
                PatchHelper.Log($"FMOD logo not found at {logoPath}");
                return null;
            }

            var bytes = File.ReadAllBytes(logoPath);
            var image = new Image();
            image.LoadPngFromBuffer(bytes);
            var texture = ImageTexture.CreateFromImage(image);

            return new TextureRect
            {
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(
                    (int)(FmodLogoWidth * scale),
                    (int)(FmodLogoHeight * scale)
                ),
            };
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to load FMOD logo: {ex.Message}");
            return null;
        }
    }
}
