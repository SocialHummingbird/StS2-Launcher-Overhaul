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

        private static readonly SaveMetricSet ProgressMetrics = new(
            FloorsClimbed,
            SumCharacterGames,
            CountDiscovered,
            TotalPlaytime
        );

        private static SaveWinner CompareProgress(string local, string cloud)
            => CompareJson(
                local,
                cloud,
                ProgressMetrics.Compare
            );

        private static int FloorsClimbed(JsonElement root)
            => GetInt(root, FloorsClimbedProperty);

        private static int TotalPlaytime(JsonElement root)
            => GetInt(root, TotalPlaytimeProperty);

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
