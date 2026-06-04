using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct AutoSyncConflictDecision
    {
        private readonly SaveComparison.SaveWinner _winner;
        private readonly Func<string, string> _message;

        private AutoSyncConflictDecision(
            SaveComparison.SaveWinner winner,
            Func<string, string> message
        )
        {
            _winner = winner;
            _message = message;
        }

        internal static AutoSyncConflictDecision ForWinner(
            SaveComparison.SaveWinner winner
        )
            => winner switch
            {
                SaveComparison.SaveWinner.Cloud => new(winner, SyncCloudWins),
                SaveComparison.SaveWinner.Local => new(winner, SyncLocalWinsUploading),
                _ => new(winner, SyncContentsDifferCloudWins),
            };

        internal Task ApplyAsync(
            AutoSyncContext sync,
            string localContent,
            string cloudContent
        )
        {
            if (_winner == SaveComparison.SaveWinner.Local)
            {
                sync.PushLocalContent(localContent, cloudContent, _message);
                return Task.CompletedTask;
            }

            return sync.PullCloudOverLocalAsync(cloudContent, _message);
        }
    }

    private static async Task SyncExistingFileAsync(
        AutoSyncContext sync,
        string localContent
    )
    {
        string cloudContent = await sync.ReadCloudContentAsync(ReadCloudFileOperation);

        if (IsCorrupt(localContent))
        {
            await sync.PullCloudOverLocalAsync(
                cloudContent,
                SyncLocalCorruptPulling
            );
            return;
        }

        if (localContent == cloudContent)
        {
            sync.Log(SyncIdenticalSkipping);
            return;
        }

        await ResolveContentConflictAsync(sync, localContent, cloudContent);
    }

    private static Task ResolveContentConflictAsync(
        AutoSyncContext sync,
        string localContent,
        string cloudContent
    )
        => AutoSyncConflictDecision
            .ForWinner(sync.GetExplicitWinner(localContent, cloudContent))
            .ApplyAsync(sync, localContent, cloudContent);

    // Save files are JSON; a non-JSON opener indicates corruption, e.g. unencrypted write.
    private static bool IsCorrupt(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;
        return content[0] != '{' && content[0] != '[';
    }
}
