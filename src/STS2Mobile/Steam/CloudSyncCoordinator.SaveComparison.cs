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

            internal SaveMetricSet(params Func<JsonElement, int>[] metrics)
            {
                _metrics = metrics;
            }

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

        private const string ActsProperty = "acts";
        private const string CharacterStatsProperty = "character_stats";
        private const string CurrentRunPathToken = "current_run";
        private const string DiscoveredActsProperty = "discovered_acts";
        private const string DiscoveredCardsProperty = "discovered_cards";
        private const string DiscoveredEventsProperty = "discovered_events";
        private const string DiscoveredPotionsProperty = "discovered_potions";
        private const string DiscoveredRelicsProperty = "discovered_relics";
        private const string FloorsClimbedProperty = "floors_climbed";
        private const string MapPointHistoryProperty = "map_point_history";
        private const string ProgressPathToken = "progress";
        private const string SaveExtension = ".save";
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
                return GetSaveKind(path) switch
                {
                    SaveKind.Progress => CompareProgress(localContent, cloudContent),
                    SaveKind.CurrentRun => CompareCurrentRun(localContent, cloudContent),
                    // History files have unique filenames; prefs have no progress concept.
                    _ => SaveWinner.None,
                };
            }
            catch (Exception ex)
            {
                PatchHelper.Log(ProgressComparisonFailed(path, ex));
                return SaveWinner.None;
            }
        }

        private static SaveKind GetSaveKind(string path)
        {
            var canonPath = CloudSavePath.Canonicalize(path).ToLowerInvariant();
            if (!canonPath.EndsWith(SaveExtension))
                return SaveKind.Other;

            if (canonPath.Contains(ProgressPathToken))
                return SaveKind.Progress;

            return canonPath.Contains(CurrentRunPathToken)
                ? SaveKind.CurrentRun
                : SaveKind.Other;
        }
    }
}
