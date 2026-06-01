using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        internal enum Result
        {
            CloudWins,
            LocalWins,
            Equal,
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

        internal static Result GetExplicitWinner(
            string path,
            string localContent,
            string cloudContent
        )
            => Compare(path, localContent, cloudContent);

        private static Result Compare(string path, string localContent, string cloudContent)
        {
            try
            {
                var canonPath = CloudSavePath.Canonicalize(path).ToLowerInvariant();
                if (canonPath.Contains(ProgressPathToken) && canonPath.EndsWith(SaveExtension))
                    return CompareProgress(localContent, cloudContent);

                if (canonPath.Contains(CurrentRunPathToken) && canonPath.EndsWith(SaveExtension))
                    return CompareCurrentRun(localContent, cloudContent);

                // History files have unique filenames; prefs have no progress concept.
                return Result.Equal;
            }
            catch (Exception ex)
            {
                PatchHelper.Log(ProgressComparisonFailed(path, ex));
                return Result.Equal;
            }
        }

        private static Result CompareNumeric(int localValue, int cloudValue)
        {
            if (localValue == cloudValue)
                return Result.Equal;

            return localValue > cloudValue ? Result.LocalWins : Result.CloudWins;
        }

        private static bool TryCompareNumeric(
            int localValue,
            int cloudValue,
            out Result result
        )
        {
            result = CompareNumeric(localValue, cloudValue);
            return result != Result.Equal;
        }
    }
}
