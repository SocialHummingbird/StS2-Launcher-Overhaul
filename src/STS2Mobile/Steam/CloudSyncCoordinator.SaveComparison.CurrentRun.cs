using System.Text.Json;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        private static readonly SaveMetricSet CurrentRunMetrics =
            SaveMetricSet.Ordered(CountRunFloors);

        private static SaveWinner CompareCurrentRun(string local, string cloud)
            => SaveJsonComparison.Compare(
                local,
                cloud,
                CurrentRunMetrics
            );

        private static int CountRunFloors(JsonElement root)
        {
            int count = 0;
            if (TryGetArray(root, MapPointHistoryProperty, out var history))
            {
                foreach (var act in history.EnumerateArray())
                {
                    if (act.ValueKind == JsonValueKind.Array)
                        count += act.GetArrayLength();
                }
            }
            else if (TryGetArray(root, ActsProperty, out var acts))
            {
                // Alternate save format.
                count = acts.GetArrayLength();
            }

            return count;
        }
    }
}
