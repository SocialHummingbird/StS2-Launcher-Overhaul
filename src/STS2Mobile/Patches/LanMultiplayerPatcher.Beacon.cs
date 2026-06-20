using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private sealed class LanBeacon
    {
        private readonly object _stateLock = new();
        private volatile bool _running;
        private Thread _thread;
        private UdpClient _udpClient;

        internal void Start()
        {
            lock (_stateLock)
            {
                if (_running)
                    return;

                _running = true;
                try
                {
                    _udpClient = new UdpClient();
                    _udpClient.EnableBroadcast = true;
                }
                catch (Exception ex)
                {
                    _running = false;
                    PatchHelper.Log($"LAN beacon failed to start: {ex.Message}");
                    return;
                }

                _thread = new Thread(SendLoop)
                {
                    IsBackground = true,
                    Name = "LanBeacon",
                };
                _thread.Start();
                PatchHelper.Log("LAN beacon started");
            }
        }

        private void SendLoop()
        {
            var endpoint = new IPEndPoint(IPAddress.Broadcast, BeaconPort);
            var message = $"{BeaconPrefix}|{GetDeviceHostname()}|{GamePort}";
            var packet = Encoding.UTF8.GetBytes(message);

            while (true)
            {
                if (!_running || _udpClient == null)
                    break;

                try
                {
                    _udpClient.Send(packet, packet.Length, endpoint);
                }
                catch when (!_running)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"Beacon send error: {ex.Message}");
                }

                for (var i = 0; i < BeaconChecksPerSendInterval && _running; i++)
                    Thread.Sleep(BeaconSendLoopSleepMs);
            }
        }

        private static string GetDeviceHostname()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch
            {
                return "Android";
            }
        }

        internal void Stop()
        {
            lock (_stateLock)
            {
                if (!_running)
                {
                    _udpClient = null;
                    return;
                }

                _running = false;
            }

            try
            {
                _udpClient?.Close();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"LAN beacon close failed: {ex.Message}");
            }
            _udpClient = null;
            _thread?.Join(500);
            PatchHelper.Log("LAN beacon stopped");
        }
    }
}
