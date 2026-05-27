using System.Text;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed class AndroidMachineInfoProvider : IMachineInfoProvider
{
    private static readonly byte[] MachineGuid = Encoding.ASCII.GetBytes("sts2launcher-android-machine-guid");
    private static readonly byte[] MacAddress = { 0x02, 0x53, 0x54, 0x53, 0x32, 0x41 };
    private static readonly byte[] DiskId = Encoding.ASCII.GetBytes("sts2launcher-android-disk");

    public byte[] GetMachineGuid() => MachineGuid;

    public byte[] GetMacAddress() => MacAddress;

    public byte[] GetDiskId() => DiskId;
}
