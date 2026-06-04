using System;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveComparison
    {
        private static readonly Func<JsonElement, int>[] CurrentRunMetrics =
        {
            CountRunFloors,
        };

        private static SaveWinner CompareCurrentRun(string local, string cloud)
            => CompareJson(
                local,
                cloud,
                (localRoot, cloudRoot) => FirstMetricWinner(
                    localRoot,
                    cloudRoot,
                    CurrentRunMetrics
                )
            );

        private static int CountRunFloors(JsonElement root)
        {
            int count = 0;
            if (
                root.TryGetProperty(MapPointHistoryProperty, out var history)
                && history.ValueKind == JsonValueKind.Array
            )
            {
                foreach (var act in history.EnumerateArray())
                {
                    if (act.ValueKind == JsonValueKind.Array)
                        count += act.GetArrayLength();
                }
            }
            else if (
                root.TryGetProperty(ActsProperty, out var acts)
                && acts.ValueKind == JsonValueKind.Array
            )
            {
                // Alternate save format.
                count = acts.GetArrayLength();
            }

            return count;
        }
    }
}
