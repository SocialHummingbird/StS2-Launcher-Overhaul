using System.Text.Json;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        private static readonly string[] DiscoveryProperties =
        {
            DiscoveredCardsProperty,
            DiscoveredRelicsProperty,
            DiscoveredPotionsProperty,
            DiscoveredEventsProperty,
            DiscoveredActsProperty,
        };

        private static Result CompareProgress(string local, string cloud)
        {
            using var localDoc = JsonDocument.Parse(local);
            using var cloudDoc = JsonDocument.Parse(cloud);
            var localRoot = localDoc.RootElement;
            var cloudRoot = cloudDoc.RootElement;

            int localFloors = GetInt(localRoot, FloorsClimbedProperty);
            int cloudFloors = GetInt(cloudRoot, FloorsClimbedProperty);
            if (TryCompareNumeric(localFloors, cloudFloors, out var result))
                return result;

            int localGames = SumCharacterGames(localRoot);
            int cloudGames = SumCharacterGames(cloudRoot);
            if (TryCompareNumeric(localGames, cloudGames, out result))
                return result;

            int localDiscovered = CountDiscovered(localRoot);
            int cloudDiscovered = CountDiscovered(cloudRoot);
            if (TryCompareNumeric(localDiscovered, cloudDiscovered, out result))
                return result;

            int localPlaytime = GetInt(localRoot, TotalPlaytimeProperty);
            int cloudPlaytime = GetInt(cloudRoot, TotalPlaytimeProperty);
            if (TryCompareNumeric(localPlaytime, cloudPlaytime, out result))
                return result;

            return Result.Equal;
        }

        private static int SumCharacterGames(JsonElement root)
        {
            int total = 0;
            if (
                root.TryGetProperty(CharacterStatsProperty, out var stats)
                && stats.ValueKind == JsonValueKind.Array
            )
            {
                foreach (var character in stats.EnumerateArray())
                {
                    total += GetInt(character, TotalWinsProperty);
                    total += GetInt(character, TotalLossesProperty);
                }
            }
            return total;
        }

        private static int CountDiscovered(JsonElement root)
        {
            int count = 0;
            foreach (var property in DiscoveryProperties)
                count += GetArrayLength(root, property);
            return count;
        }
    }
}
