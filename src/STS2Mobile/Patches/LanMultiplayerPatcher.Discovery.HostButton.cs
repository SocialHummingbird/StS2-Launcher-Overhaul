using System;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private sealed partial class LanDiscovery
    {
        private void AddHostButton(string ip, string hostname, int port)
        {
            try
            {
                var fakeId = (ulong)(uint)ip.GetHashCode();
                var button = (Node)_joinFriendButtonCreate.Invoke(null, new object[] { fakeId });
                _buttonContainer.AddChild(button);

                try
                {
                    var textNode = button.GetNode("%Text");
                    textNode.Set("text", $"[center]{hostname}\n{ip}:{port}[/center]");
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"Text override failed for {ip}: {ex.Message}");
                }

                var capturedIp = ip;
                var capturedPort = port;
                button.Connect(
                    "Released",
                    Callable.From<Control>(_ =>
                    {
                        SaveLastIp(capturedIp);
                        JoinViaIp(_screen, capturedIp, capturedPort);
                    })
                );

                _hostButtons[ip] = button;
                PatchHelper.Log($"Discovered LAN host: {hostname} @ {ip}:{port}");
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"AddHostButton error: {ex.Message}");
            }
        }
    }
}
