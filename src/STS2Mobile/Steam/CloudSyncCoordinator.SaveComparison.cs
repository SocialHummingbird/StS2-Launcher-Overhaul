using System;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        internal enum SaveWinner
        {
            None,
            Cloud,
            Local,
        }

        private readonly struct SaveMetricSet
        {
            private readonly Func<JsonElement, int>[] _metrics;

            private SaveMetricSet(params Func<JsonElement, int>[] metrics)
            {
                _metrics = metrics;
            }

            internal static SaveMetricSet Ordered(
                params Func<JsonElement, int>[] metrics
            )
                => new(metrics);

            internal SaveWinner Compare(JsonElement localRoot, JsonElement cloudRoot)
            {
                foreach (var metric in _metrics)
                {
                    var winner = NumericWinner(metric(localRoot), metric(cloudRoot));
                    if (winner != SaveWinner.None)
                        return winner;
                }

                return SaveWinner.None;
            }
        }

        private static SaveWinner NumericWinner(int local, int cloud)
        {
            if (local == cloud)
                return SaveWinner.None;

            return local > cloud ? SaveWinner.Local : SaveWinner.Cloud;
        }

        private const string ActsProperty = "acts";
        private const string CharacterStatsProperty = "character_stats";
        private const string DiscoveredActsProperty = "discovered_acts";
        private const string DiscoveredCardsProperty = "discovered_cards";
        private const string DiscoveredEventsProperty = "discovered_events";
        private const string DiscoveredPotionsProperty = "discovered_potions";
        private const string DiscoveredRelicsProperty = "discovered_relics";
        private const string FloorsClimbedProperty = "floors_climbed";
        private const string MapPointHistoryProperty = "map_point_history";
        private const string TotalLossesProperty = "total_losses";
        private const string TotalPlaytimeProperty = "total_playtime";
        private const string TotalWinsProperty = "total_wins";
    }
}
