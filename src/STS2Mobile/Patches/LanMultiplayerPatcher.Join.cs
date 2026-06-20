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

        var (ip, port) = ParseIpPort(raw);
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
            var connInit = _eNetClientConnInitCtor.Invoke(
                new object[] { Player2Id, ip, (ushort)port }
            );
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
            if (
                parts.Length == 2
                && int.TryParse(parts[1], out int port)
                && port >= MinPort
                && port <= MaxPort
            )
                return (parts[0], port);
        }

        return (input, GamePort);
    }
}
