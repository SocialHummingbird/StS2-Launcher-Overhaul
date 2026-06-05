using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly partial struct AutoSyncContext
    {
        private readonly struct ContentConflictResolution
        {
            private ContentConflictResolution(
                SaveComparison.SaveWinner winner,
                Func<string, string> cloudMessage
            )
            {
                Winner = winner;
                CloudMessage = cloudMessage;
            }

            private SaveComparison.SaveWinner Winner { get; }
            private Func<string, string> CloudMessage { get; }
            private bool LocalWins => Winner == SaveComparison.SaveWinner.Local;

            internal static ContentConflictResolution FromWinner(
                SaveComparison.SaveWinner winner
            )
                => new(
                    winner,
                    winner == SaveComparison.SaveWinner.Cloud
                        ? SyncCloudWins
                        : SyncContentsDifferCloudWins
                );

            internal Task ApplyAsync(
                AutoSyncContext sync,
                string localContent,
                string cloudContent
            )
            {
                if (LocalWins)
                {
                    sync.PushLocalContent(
                        localContent,
                        cloudContent,
                        SyncLocalWinsUploading
                    );
                    return Task.CompletedTask;
                }

                return sync.PullCloudOverLocalAsync(cloudContent, CloudMessage);
            }
        }

        private async Task SyncExistingFileAsync(string localContent)
        {
            string cloudContent = await ReadCloudContentAsync(ReadCloudFileOperation);

            if (IsCorrupt(localContent))
            {
                await PullCloudOverLocalAsync(
                    cloudContent,
                    SyncLocalCorruptPulling
                );
                return;
            }

            if (localContent == cloudContent)
            {
                Log(SyncIdenticalSkipping);
                return;
            }

            await ResolveContentConflictAsync(localContent, cloudContent);
        }

        private Task ResolveContentConflictAsync(
            string localContent,
            string cloudContent
        )
        {
            var resolution = ContentConflictResolution.FromWinner(
                GetExplicitWinner(localContent, cloudContent)
            );
            return resolution.ApplyAsync(this, localContent, cloudContent);
        }

        // Save files are JSON; a non-JSON opener indicates corruption, e.g. unencrypted write.
        private static bool IsCorrupt(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;
            return content[0] != '{' && content[0] != '[';
        }
    }
}
