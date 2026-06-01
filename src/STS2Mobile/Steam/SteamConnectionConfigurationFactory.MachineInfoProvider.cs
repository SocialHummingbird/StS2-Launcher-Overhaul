using System.Text;
using SteamKit2;

namespace STS2Mobile.Steam;

internal static partial class SteamConnectionConfigurationFactory
{
    private static readonly IMachineInfoProvider AndroidMachineInfo =
        new AndroidMachineInfoProvider();

    private sealed class AndroidMachineInfoProvider : IMachineInfoProvider
    {
        private const string DiskId = "sts2launcher-android-disk";
        private const string MachineGuid = "sts2launcher-android-machine-guid";

        private static readonly byte[] MacAddress =
        {
            0x02,
            0x53,
            0x54,
            0x53,
            0x32,
            0x41,
        };

        private AndroidMachineInfoProvider() { }

        public byte[] GetMachineGuid() => Encoding.ASCII.GetBytes(MachineGuid);

        public byte[] GetMacAddress() => (byte[])MacAddress.Clone();

        public byte[] GetDiskId() => Encoding.ASCII.GetBytes(DiskId);
    }
}
