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

        private readonly struct SaveComparisonInput
        {
            internal SaveComparisonInput(
                string path,
                string localContent,
                string cloudContent
            )
            {
                Path = path;
                LocalContent = localContent;
                CloudContent = cloudContent;
            }

            internal string Path { get; }
            internal string LocalContent { get; }
            internal string CloudContent { get; }
            internal SaveKind Kind => GetSaveKind(Path);

            internal SaveWinner Compare()
            {
                try
                {
                    return CompareByKind();
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(SaveComparisonFailed(Path, ex));
                    return SaveWinner.None;
                }
            }

            private SaveWinner CompareByKind()
                => Kind switch
                {
                    SaveKind.Progress => CompareProgress(LocalContent, CloudContent),
                    SaveKind.CurrentRun => CompareCurrentRun(LocalContent, CloudContent),
                    _ => SaveWinner.None,
                };
        }

        internal static SaveWinner GetExplicitWinner(
            string path,
            string localContent,
            string cloudContent
        )
            => new SaveComparisonInput(path, localContent, cloudContent)
                .Compare();

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
