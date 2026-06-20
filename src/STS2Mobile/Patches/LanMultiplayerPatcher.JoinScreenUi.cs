using System;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private static LineEdit ApplyJoinScreenUi(
        Node screen,
        object noFriendsLabel,
        Action onManualJoinPressed)
    {
        var titleLabel = screen.GetNode("TitleLabel");
        _setTextAutoSize?.Invoke(titleLabel, new object[] { "JOIN LAN GAME" });

        if (noFriendsLabel != null)
            _setTextAutoSize?.Invoke(
                noFriendsLabel,
                new object[] { "Searching for LAN hosts..." }
            );

        var ipEdit = CreateIpEdit();
        var joinButton = CreateJoinButton();
        var ipContainer = CreateIpContainer(ipEdit, joinButton);

        screen.AddChild(ipContainer);
        ConnectJoinActions(ipEdit, joinButton, onManualJoinPressed);

        return ipEdit;
    }

    private static LineEdit CreateIpEdit()
    {
        var ipEdit = new LineEdit();
        ipEdit.PlaceholderText = "Enter host IP address";
        ipEdit.Text = LoadLastIp();
        ipEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        ipEdit.AddThemeFontSizeOverride("font_size", EntryFontSize);
        return ipEdit;
    }

    private static Button CreateJoinButton()
    {
        var joinButton = new Button();
        joinButton.Text = "JOIN";
        joinButton.CustomMinimumSize = new Vector2(100, 0);
        joinButton.AddThemeFontSizeOverride("font_size", EntryFontSize);
        return joinButton;
    }

    private static HBoxContainer CreateIpContainer(LineEdit ipEdit, Button joinButton)
    {
        var ipContainer = new HBoxContainer();
        ipContainer.Name = "LanIpEntry";
        ipContainer.AddThemeConstantOverride("separation", ContainerSeparation);
        ipContainer.AnchorLeft = 0.15f;
        ipContainer.AnchorRight = 0.85f;
        ipContainer.AnchorTop = 1.0f;
        ipContainer.AnchorBottom = 1.0f;
        ipContainer.OffsetTop = -100;
        ipContainer.OffsetBottom = -20;
        ipContainer.AddChild(ipEdit);
        ipContainer.AddChild(joinButton);
        return ipContainer;
    }

    private static void ConnectJoinActions(
        LineEdit ipEdit,
        Button joinButton,
        Action onManualJoinPressed)
    {
        joinButton.Connect("pressed", Callable.From(onManualJoinPressed));
        ipEdit.Connect(
            "text_submitted",
            Callable.From<string>(_ => onManualJoinPressed())
        );
    }
}
