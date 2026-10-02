using System;
using Godot;

namespace STS2Mobile.Patches;
internal static partial class LanMultiplayerPatcher
{
    private static void OnManualJoinPressed(object screen)
    {
        if (_ipLineEdit == null || !GodotObject.IsInstanceValid(_ipLineEdit))
            return;
        var raw = _ipLineEdit.Text.Trim();
        if (string.IsNullOrEmpty(raw))
            return;
        var(ip, port) = ParseIpPort(raw);
        SaveLastIp(raw);
        JoinViaIp(screen, ip, port);
    }

    private static void JoinViaIp(object screen, string ip, int port)
    {
        if (_joinInProgress)
            return;
        _joinInProgress = true;
        try
        {
            var connInit = _eNetClientConnInitCtor.Invoke(new object[] { Player2Id, ip, (ushort)port });
            var task = _joinGameAsyncMethod.Invoke(screen, new object[] { connInit });
            _taskHelperRunSafely?.Invoke(null, new object[] { task });
            PatchHelper.Log($"Joining LAN game at {ip}:{port}");
        }
        catch (Exception ex)
        {
            _joinInProgress = false;
            PatchHelper.Log($"JoinViaIp error: {ex}");
        }
    }

    private static (string ip, int port) ParseIpPort(string input)
    {
        if (input.Contains(':'))
        {
            var parts = input.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int port) && port >= MinPort && port <= MaxPort)
                return (parts[0], port);
        }

        return (input, GamePort);
    }

    private static LineEdit ApplyJoinScreenUi(Node screen, object noFriendsLabel, Action onManualJoinPressed)
    {
        var titleLabel = screen.GetNode("TitleLabel");
        _setTextAutoSize?.Invoke(titleLabel, new object[] { "JOIN LAN GAME" });
        if (noFriendsLabel != null)
            _setTextAutoSize?.Invoke(noFriendsLabel, new object[] { "Searching for LAN hosts..." });
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

    private static void ConnectJoinActions(LineEdit ipEdit, Button joinButton, Action onManualJoinPressed)
    {
        joinButton.Connect("pressed", Callable.From(onManualJoinPressed));
        ipEdit.Connect("text_submitted", Callable.From<string>(_ => onManualJoinPressed()));
    }

    private static void UpdateScreenContext()
    {
        try
        {
            var instance = _activeScreenContextInstance?.GetValue(null);
            _activeScreenContextUpdate?.Invoke(instance, null);
        }
        catch (Exception ex)
        {
            if (!_screenContextUpdateFailureLogged)
            {
                _screenContextUpdateFailureLogged = true;
                PatchHelper.Log($"LAN screen context update failed: {ex.Message}");
            }
        }
    }

    private static string LoadLastIp()
    {
        try
        {
            var config = new ConfigFile();
            if (config.Load(LastIpConfigPath) == Error.Ok)
                return (string)config.GetValue(LastIpSection, LastIpKey, "");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to load LAN last IP config: {ex.Message}");
        }

        return "";
    }

    private static void SaveLastIp(string ip)
    {
        try
        {
            var config = new ConfigFile();
            config.SetValue(LastIpSection, LastIpKey, ip);
            config.Save(LastIpConfigPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to save LAN last IP config: {ex.Message}");
        }
    }
}
