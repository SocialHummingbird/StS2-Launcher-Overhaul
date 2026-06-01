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

        private static SaveWinner CompareProgress(string local, string cloud)
        {
            using var localDoc = JsonDocument.Parse(local);
            using var cloudDoc = JsonDocument.Parse(cloud);
            var localRoot = localDoc.RootElement;
            var cloudRoot = cloudDoc.RootElement;

            return FirstNumericWinner(
                NumericComparison.Of(
                    GetInt(localRoot, FloorsClimbedProperty),
                    GetInt(cloudRoot, FloorsClimbedProperty)
                ),
                NumericComparison.Of(SumCharacterGames(localRoot), SumCharacterGames(cloudRoot)),
                NumericComparison.Of(CountDiscovered(localRoot), CountDiscovered(cloudRoot)),
                NumericComparison.Of(
                    GetInt(localRoot, TotalPlaytimeProperty),
                    GetInt(cloudRoot, TotalPlaytimeProperty)
                )
            );
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
