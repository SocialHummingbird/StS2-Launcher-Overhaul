using System;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudWriteQueue
    {
        private void ProcessLoop()
        {
            foreach (var action in _queue.GetConsumingEnumerable())
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(BackgroundWriteFailed(ex));
                }
                finally
                {
                    MarkCompleted();
                }
            }
        }
    }
}
