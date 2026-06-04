using System;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        private static SaveWinner CompareJson(
            string local,
            string cloud,
            Func<JsonElement, JsonElement, SaveWinner> compare
        )
        {
            using var localDoc = JsonDocument.Parse(local);
            using var cloudDoc = JsonDocument.Parse(cloud);
            return compare(localDoc.RootElement, cloudDoc.RootElement);
        }

        private static int GetInt(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result)
                ? result
                : 0;
        }

        private static int GetArrayLength(JsonElement element, string property)
        {
            return
                TryGetArray(element, property, out var value)
                ? value.GetArrayLength()
                : 0;
        }

        private static bool TryGetArray(
            JsonElement element,
            string property,
            out JsonElement array
        )
        {
            if (
                element.TryGetProperty(property, out array)
                && array.ValueKind == JsonValueKind.Array
            )
                return true;

            array = default;
            return false;
        }
    }
}
