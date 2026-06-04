using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
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
