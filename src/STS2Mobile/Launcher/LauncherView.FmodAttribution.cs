using System;
using System.IO;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private sealed class FmodAttributionSection : VBoxContainer
    {
        private const string CreditText = "Made using FMOD Studio by Firelight Technologies Pty Ltd.";
        private const int CreditFontSize = 8;
        private const string LogoFileName = "fmod_logo.png";
        private const int LogoHeight = 30;
        private const int LogoWidth = 120;
        private static readonly Color CreditColor = new(0.5f, 0.5f, 0.55f);

        internal FmodAttributionSection(float scale)
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            Alignment = BoxContainer.AlignmentMode.End;

            var logo = LoadLogo(scale);
            if (logo != null)
                AddChild(logo);

            var credit = new StyledLabel(
                CreditText,
                scale,
                fontSize: CreditFontSize
            );
            credit.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                CreditColor
            );
            AddChild(credit);
        }

        private static TextureRect LoadLogo(float scale)
        {
            try
            {
                var logoPath = Path.Combine(OS.GetDataDir(), LogoFileName);
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
                        (int)(LogoWidth * scale),
                        (int)(LogoHeight * scale)
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
}
