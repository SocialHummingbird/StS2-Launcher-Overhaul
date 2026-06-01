using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        internal readonly struct SaveWinner
        {
            private enum Winner
            {
                None,
                Cloud,
                Local,
            }

            private readonly Winner _winner;

            private SaveWinner(Winner winner)
            {
                _winner = winner;
            }

            internal bool CloudWins => _winner == Winner.Cloud;
            internal bool LocalWins => _winner == Winner.Local;
            internal bool HasWinner => _winner != Winner.None;

            internal static SaveWinner Cloud()
                => new(Winner.Cloud);

            internal static SaveWinner Local()
                => new(Winner.Local);

            internal static SaveWinner None()
                => new(Winner.None);
        }

        private readonly struct NumericComparison
        {
            private NumericComparison(int local, int cloud)
            {
                Local = local;
                Cloud = cloud;
            }

            internal int Local { get; }
            internal int Cloud { get; }

            internal static NumericComparison Of(int local, int cloud)
                => new(local, cloud);
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
                var canonPath = CloudSavePath.Canonicalize(path).ToLowerInvariant();
                if (canonPath.Contains(ProgressPathToken) && canonPath.EndsWith(SaveExtension))
                    return CompareProgress(localContent, cloudContent);

                if (canonPath.Contains(CurrentRunPathToken) && canonPath.EndsWith(SaveExtension))
                    return CompareCurrentRun(localContent, cloudContent);

                // History files have unique filenames; prefs have no progress concept.
                return SaveWinner.None();
            }
            catch (Exception ex)
            {
                PatchHelper.Log(ProgressComparisonFailed(path, ex));
                return SaveWinner.None();
            }
        }

        private static SaveWinner CompareNumeric(
            int localValue,
            int cloudValue
        )
        {
            if (localValue == cloudValue)
                return SaveWinner.None();

            return localValue > cloudValue ? SaveWinner.Local() : SaveWinner.Cloud();
        }

        private static SaveWinner FirstNumericWinner(params NumericComparison[] comparisons)
        {
            foreach (var comparison in comparisons)
            {
                var winner = CompareNumeric(comparison.Local, comparison.Cloud);
                if (winner.HasWinner)
                    return winner;
            }

            return SaveWinner.None();
        }
    }
}
