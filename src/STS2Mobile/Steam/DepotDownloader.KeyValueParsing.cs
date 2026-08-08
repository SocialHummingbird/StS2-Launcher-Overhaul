using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static uint? ReadKeyValueUInt32(KeyValue value)
        => value != KeyValue.Invalid
            && value.Value != null
            && uint.TryParse(value.Value, out var parsed)
            ? parsed
            : null;

    private static ulong? ReadKeyValueUInt64(KeyValue value)
        => value != KeyValue.Invalid
            && value.Value != null
            && ulong.TryParse(value.Value, out var parsed)
            ? parsed
            : null;
}
