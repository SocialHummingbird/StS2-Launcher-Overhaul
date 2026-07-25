using System;
using System.Collections.Generic;
using Godot;

namespace LauncherUiPreview;

internal static class LauncherCloudProgressPreviewValidator
{
    internal static void Validate(Control root, string fixture)
    {
        switch (fixture.Trim().ToLowerInvariant())
        {
            case "pull-transfer":
                ExpectVisibleLabel(root, "Downloading Steam Cloud saves");
                ExpectVisibleLabel(root, "11/24 checked");
                ExpectVisibleLabel(root, "8 downloaded");
                ExpectVisibleLabel(root, "Current: profile2/saves/progress.save");
                ExpectProgress(root, minimum: 35, maximum: 85);
                ExpectButtonVisibility(
                    root,
                    "Cancel Cloud Operation",
                    expectedVisible: true
                );
                break;
            case "pull-complete":
                ExpectVisibleLabel(root, "Pull complete");
                ExpectVisibleLabel(root, "24/24 checked");
                ExpectVisibleLabel(root, "18 downloaded");
                ExpectVisibleLabel(root, "1 failed");
                ExpectVisibleLabel(root, "4 modded file(s) seeded");
                ExpectProgress(root, minimum: 100, maximum: 100);
                ExpectButtonVisibility(
                    root,
                    "Cancel Cloud Operation",
                    expectedVisible: false
                );
                break;
        }
    }

    private static void ExpectVisibleLabel(Node root, string expectedText)
    {
        foreach (var label in Descendants<Label>(root))
        {
            if (label.IsVisibleInTree()
                && label.Text.Contains(
                    expectedText,
                    StringComparison.Ordinal
                ))
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"Visible cloud progress text was not found: {expectedText}"
        );
    }

    private static void ExpectProgress(
        Node root,
        double minimum,
        double maximum
    )
    {
        foreach (var progress in Descendants<ProgressBar>(root))
        {
            if (!progress.IsVisibleInTree())
                continue;

            if (progress.Value >= minimum && progress.Value <= maximum)
                return;
        }

        throw new InvalidOperationException(
            $"Visible cloud progress bar was not in range {minimum:0}-{maximum:0}."
        );
    }

    private static void ExpectButtonVisibility(
        Node root,
        string expectedText,
        bool expectedVisible
    )
    {
        foreach (var button in Descendants<Button>(root))
        {
            if (
                button.Text.Contains(expectedText, StringComparison.Ordinal)
                && button.IsVisibleInTree() == expectedVisible
            )
            {
                if (expectedVisible && button.Disabled)
                {
                    throw new InvalidOperationException(
                        $"Visible cloud action was disabled: {expectedText}"
                    );
                }

                return;
            }
        }

        throw new InvalidOperationException(
            $"Cloud action visibility was not {expectedVisible}: {expectedText}"
        );
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match)
                yield return match;
            foreach (var descendant in Descendants<T>(child))
                yield return descendant;
        }
    }
}
