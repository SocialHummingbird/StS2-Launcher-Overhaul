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
                return CompareByKind(
                    GetSaveKind(path),
                    localContent,
                    cloudContent
                );
            }
            catch (Exception ex)
            {
                PatchHelper.Log(SaveComparisonFailed(path, ex));
                return SaveWinner.None;
            }
        }

        private static SaveWinner CompareByKind(
            SaveKind kind,
            string localContent,
            string cloudContent
        )
            => kind switch
            {
                SaveKind.Progress => CompareProgress(localContent, cloudContent),
                SaveKind.CurrentRun => CompareCurrentRun(localContent, cloudContent),
                _ => SaveWinner.None,
            };

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
