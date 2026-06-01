using System.Text.Json;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        private static int GetInt(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result)
                ? result
                : 0;
        }

        private static int GetArrayLength(JsonElement element, string property)
        {
            return
                element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.Array
                ? value.GetArrayLength()
                : 0;
        }
    }
}
