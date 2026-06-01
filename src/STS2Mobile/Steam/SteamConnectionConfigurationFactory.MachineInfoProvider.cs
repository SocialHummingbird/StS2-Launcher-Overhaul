using System.Text;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private sealed class AndroidMachineInfoProvider : IMachineInfoProvider
    {
        internal AndroidMachineInfoProvider() { }

        private static readonly byte[] MacAddress =
        {
            0x02,
            0x53,
            0x54,
            0x53,
            0x32,
            0x41,
        };

        public byte[] GetMachineGuid() => Encoding.ASCII.GetBytes("sts2launcher-android-machine-guid");

        public byte[] GetMacAddress() => (byte[])MacAddress.Clone();

        public byte[] GetDiskId() => Encoding.ASCII.GetBytes("sts2launcher-android-disk");
    }
}
