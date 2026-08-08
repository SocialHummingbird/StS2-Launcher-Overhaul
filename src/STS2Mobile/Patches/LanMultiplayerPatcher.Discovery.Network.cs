using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private sealed partial class LanDiscovery
    {
        private void ListenLoop()
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                if (!_running || _udpClient == null)
                    break;

                try
                {
                    var data = _udpClient.Receive(ref ep);
                    var msg = Encoding.UTF8.GetString(data);
                    var parts = msg.Split('|');
                    if (parts.Length >= 3 && parts[0] == BeaconPrefix)
                    {
                        var ip = ep.Address.ToString();

                        if (_localIps.Contains(ip))
                            continue;

                        var hostname = parts[1];
                        if (int.TryParse(parts[2], out int port))
                        {
                            lock (_lock)
                            {
                                _hosts[ip] = (hostname, port, DateTime.UtcNow);
                            }
                        }
                    }
                }
                catch (SocketException) when (!_running)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                        PatchHelper.Log($"Discovery recv error: {ex.Message}");
                }
            }
        }

        private static HashSet<string> GetLocalIps()
        {
            var ips = new HashSet<string>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;

                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            ips.Add(addr.Address.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"LAN local IP enumeration failed: {ex.Message}");
            }

            return ips;
        }
    }
}
