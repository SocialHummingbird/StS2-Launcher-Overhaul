using Godot;

namespace STS2Mobile;

public static partial class ModEntry
{
    private static void AddMinimalBootstrapUi(SceneTree tree)
    {
        var root = new Control
        {
            Name = "STS2MobileMinimalBootstrapUi",
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Name = "STS2MobileMinimalBootstrapBackground",
            Color = new Color(0.02f, 0.025f, 0.03f, 1f),
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(background);

        tree.Root.AddChild(root);
        PatchHelper.Log("Minimal bootstrap UI displayed");
    }

    private static void AddPlainControlsBootstrapUi(SceneTree tree)
    {
        var root = new Control
        {
            Name = "STS2MobilePlainControlsBootstrapUi",
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Name = "STS2MobilePlainControlsBackground",
            Color = new Color(0.02f, 0.025f, 0.03f, 1f),
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(background);

        var panel = new VBoxContainer
        {
            Name = "STS2MobilePlainControlsPanel",
            Position = new Vector2(80f, 80f),
            Size = new Vector2(900f, 900f),
        };
        root.AddChild(panel);

        panel.AddChild(new Label
        {
            Text = "StS2 Mobile bootstrap controls probe",
        });

        panel.AddChild(new LineEdit
        {
            PlaceholderText = "Username field probe",
        });

        panel.AddChild(new Button
        {
            Text = "Button probe",
        });

        var dropdown = new OptionButton();
        dropdown.AddItem("public");
        dropdown.AddItem("public-beta");
        panel.AddChild(dropdown);

        var log = new RichTextLabel
        {
            Text = "RichTextLabel probe\nIf this screen stays open, built-in text controls are safe.",
            CustomMinimumSize = new Vector2(800f, 300f),
        };
        panel.AddChild(log);

        tree.Root.AddChild(root);
        PatchHelper.Log("Plain controls bootstrap UI displayed");
    }

    private static void AddStyledControlsBootstrapUi(SceneTree tree)
    {
        var root = new Control
        {
            Name = "STS2MobileStyledControlsBootstrapUi",
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Name = "STS2MobileStyledControlsBackground",
            Color = Launcher.Components.LauncherComponentTheme.ScreenBackground,
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(background);

        var panel = new Launcher.Components.StyledPanel(1f, widthRatio: 0.9f, compact: false);
        panel.UpdateSizeFromViewport(new Vector2(1920f, 1730f), 0.88f);
        root.AddChild(panel);

        var content = new VBoxContainer();
        content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddThemeConstantOverride("separation", 12);
        panel.AddContent(content);

        content.AddChild(new Launcher.Components.StyledLabel(
            "Styled launcher controls probe",
            1f,
            fontSize: 24,
            align: HorizontalAlignment.Left
        ));
        content.AddChild(new Launcher.Sections.LoginSection(1f, compact: false));
        content.AddChild(new Launcher.Sections.DownloadSection(1f, compact: false));
        content.AddChild(new Launcher.Sections.ActionSection(1f, compact: false));

        var log = new RichTextLabel
        {
            Text = "Styled controls probe\nIf this stays open, styled sections are safe outside the full launcher view.",
            CustomMinimumSize = new Vector2(1000f, 220f),
        };
        content.AddChild(log);

        tree.Root.AddChild(root);
        PatchHelper.Log("Styled controls bootstrap UI displayed");
    }
}
