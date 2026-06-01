using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
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

        public static (bool CloudWins, bool LocalWins) GetExplicitWinner(
            string path,
            string localContent,
            string cloudContent
        )
            => Compare(path, localContent, cloudContent);

        private static (bool CloudWins, bool LocalWins) Compare(
            string path,
            string localContent,
            string cloudContent
        )
        {
            try
            {
                var canonPath = CloudSavePath.Canonicalize(path).ToLowerInvariant();
                if (canonPath.Contains(ProgressPathToken) && canonPath.EndsWith(SaveExtension))
                    return CompareProgress(localContent, cloudContent);

                if (canonPath.Contains(CurrentRunPathToken) && canonPath.EndsWith(SaveExtension))
                    return CompareCurrentRun(localContent, cloudContent);

                // History files have unique filenames; prefs have no progress concept.
                return NoWinner();
            }
            catch (Exception ex)
            {
                PatchHelper.Log(ProgressComparisonFailed(path, ex));
                return NoWinner();
            }
        }

        private static (bool CloudWins, bool LocalWins) CompareNumeric(
            int localValue,
            int cloudValue
        )
        {
            if (localValue == cloudValue)
                return NoWinner();

            return localValue > cloudValue ? LocalWins() : CloudWins();
        }

        private static (bool CloudWins, bool LocalWins) FirstNumericWinner(
            params (int Local, int Cloud)[] comparisons
        )
        {
            foreach (var comparison in comparisons)
            {
                var winner = CompareNumeric(comparison.Local, comparison.Cloud);
                if (HasWinner(winner))
                    return winner;
            }

            return NoWinner();
        }

        private static bool HasWinner((bool CloudWins, bool LocalWins) winner)
            => winner.CloudWins || winner.LocalWins;

        private static (bool CloudWins, bool LocalWins) CloudWins()
            => (CloudWins: true, LocalWins: false);

        private static (bool CloudWins, bool LocalWins) LocalWins()
            => (CloudWins: false, LocalWins: true);

        private static (bool CloudWins, bool LocalWins) NoWinner()
            => (CloudWins: false, LocalWins: false);
    }
}
