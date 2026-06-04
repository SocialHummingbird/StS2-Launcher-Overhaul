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

        private enum SaveKind
        {
            Other,
            Progress,
            CurrentRun,
        }

        private readonly struct SaveComparisonPlan
        {
            private readonly Func<string, string, SaveWinner> _compare;

            private SaveComparisonPlan(Func<string, string, SaveWinner> compare)
            {
                _compare = compare;
            }

            internal static SaveComparisonPlan ForPath(string path)
                => GetSaveKind(path) switch
                {
                    SaveKind.Progress => new(CompareProgress),
                    SaveKind.CurrentRun => new(CompareCurrentRun),
                    _ => new(NoExplicitWinner),
                };

            internal SaveWinner Compare(string localContent, string cloudContent)
                => _compare(localContent, cloudContent);

            private static SaveWinner NoExplicitWinner(string _, string __)
                => SaveWinner.None;
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

        internal static SaveWinner GetExplicitWinner(
            string path,
            string localContent,
            string cloudContent
        )
            => Compare(path, localContent, cloudContent);

        private static SaveWinner Compare(
            string path,
            string localContent,
            string cloudContent
        )
        {
            try
            {
                return SaveComparisonPlan
                    .ForPath(path)
                    .Compare(localContent, cloudContent);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(ProgressComparisonFailed(path, ex));
                return SaveWinner.None;
            }
        }

        private static SaveKind GetSaveKind(string path)
        {
            if (CloudSavePath.IsProgressSave(path))
                return SaveKind.Progress;

            return CloudSavePath.IsCurrentRunSave(path)
                ? SaveKind.CurrentRun
                : SaveKind.Other;
        }
    }
}
