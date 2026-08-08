using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal sealed class AndroidMainMenuWorkingSetPlan
{
    private AndroidMainMenuWorkingSetPlan(
        IReadOnlyList<AndroidMainMenuResource> resources,
        IReadOnlyList<string> missingLogicalPaths,
        int duplicateCount,
        int rejectedCount
    )
    {
        Resources = resources;
        MissingLogicalPaths = missingLogicalPaths;
        DuplicateCount = duplicateCount;
        RejectedCount = rejectedCount;
    }

    internal IReadOnlyList<AndroidMainMenuResource> Resources { get; }
    internal IReadOnlyList<string> MissingLogicalPaths { get; }
    internal int DuplicateCount { get; }
    internal int RejectedCount { get; }

    internal static AndroidMainMenuWorkingSetPlan Create(
        IEnumerable<AndroidMainMenuResource> configuredResources,
        Func<string, bool> activeVirtualResourceExists
    )
    {
        var resources = new List<AndroidMainMenuResource>();
        var missing = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicateCount = 0;
        var rejectedCount = 0;

        foreach (var resource in configuredResources)
        {
            var logicalPath = resource.LogicalPath;
            if (!IsLogicalResourcePath(logicalPath))
            {
                rejectedCount++;
                continue;
            }

            if (!seen.Add(logicalPath))
            {
                duplicateCount++;
                continue;
            }

            if (activeVirtualResourceExists(logicalPath))
                resources.Add(resource);
            else
                missing.Add(logicalPath);
        }

        return new AndroidMainMenuWorkingSetPlan(
            resources,
            missing,
            duplicateCount,
            rejectedCount
        );
    }

    internal static bool IsLogicalResourcePath(string path)
        => !string.IsNullOrWhiteSpace(path)
            && path.StartsWith("res://", StringComparison.Ordinal)
            && !path.StartsWith("res://.godot/", StringComparison.OrdinalIgnoreCase)
            && path.IndexOf("/../", StringComparison.Ordinal) < 0
            && !path.EndsWith("/..", StringComparison.Ordinal)
            && path.IndexOf('\\') < 0;
}
