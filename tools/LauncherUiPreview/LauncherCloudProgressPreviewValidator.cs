using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LauncherUiPreview;

internal static class LauncherCloudProgressPreviewValidator
{
    internal static void Validate(
        Control root,
        string fixture,
        string destination
    )
    {
        switch (fixture.Trim().ToLowerInvariant())
        {
            case "ready" when destination.Equals(
                "home",
                StringComparison.OrdinalIgnoreCase
            ):
                ExpectButtonState(root, "Start Game", disabled: false);
                ExpectManualTransfersHidden(root);
                break;
            case "pull-transfer":
                ExpectVisibleLabel(root, "Downloading Steam Cloud saves");
                ExpectVisibleLabel(root, "11/24 checked");
                ExpectVisibleLabel(root, "11 verified");
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
                ExpectVisibleLabel(root, "24 verified");
                ExpectProgress(root, minimum: 100, maximum: 100);
                ExpectButtonVisibility(
                    root,
                    "Cancel Cloud Operation",
                    expectedVisible: false
                );
                break;
            case "sync-source-choice":
                ExpectVisibleLabel(root, "No trusted baseline exists");
                ExpectVisibleLabel(root, "Use Android uploads the Android copy");
                ExpectVisibleLabel(root, "Use Steam backs up Android first");
                ExpectButtonState(root, "Use Android", disabled: false);
                ExpectButtonState(root, "Use Steam", disabled: false);
                ExpectButtonState(root, "Start Game", disabled: true);
                ExpectManualTransfersHidden(root);
                break;
            case "sync-reconciling":
                ExpectVisibleLabel(
                    root,
                    "Reconciling Android and Steam saves before launch"
                );
                ExpectButtonState(root, "Start Game", disabled: true);
                ExpectManualTransfersHidden(root);
                break;
            case "sync-conflict":
                ExpectVisibleLabel(
                    root,
                    "Launch blocked by an Android/Steam save conflict"
                );
                ExpectButtonState(root, "Start Game", disabled: false);
                ExpectManualTransfersHidden(root);
                break;
            case "sync-offline-pending":
                ExpectVisibleLabel(
                    root,
                    "Pending automatic save sync remains on disk"
                );
                ExpectButtonState(root, "Start Game", disabled: false);
                ExpectManualTransfersHidden(root);
                break;
            case "recovery-empty":
                ExpectVisibleLabel(root, "No local recovery copy is currently available");
                ExpectButtonState(root, "Scan for Recovery Copies", disabled: false);
                ExpectButtonState(root, "Export Current Android Saves", disabled: false);
                ExpectButtonState(root, "Restore on Android", disabled: true);
                ExpectButtonVisibility(root, "Undo Last Restore", expectedVisible: false);
                ExpectButtonVisibility(
                    root,
                    "Approve Validated Save for Sync",
                    expectedVisible: false
                );
                break;
            case "recovery-unknown":
                ExpectVisibleLabel(root, "Account: Unknown");
                ExpectVisibleLabel(root, "Game version: Unknown");
                ExpectButtonState(root, "Restore on Android", disabled: false);
                ExpectButtonVisibility(root, "Undo Last Restore", expectedVisible: false);
                break;
            case "recovery-confirm":
                ExpectVisibleLabel(root, "Hashes: verified");
                ExpectVisibleLabel(root, "Steam will not be changed");
                ExpectButtonState(root, "Export Recovery Bundle", disabled: false);
                ExpectButtonState(root, "Restore on Android", disabled: false);
                break;
            case "recovery-restored":
                ExpectVisibleLabel(root, "Restored and verified on Android");
                ExpectVisibleLabel(root, "Steam was not changed");
                ExpectButtonVisibility(root, "Undo Last Restore", expectedVisible: true);
                ExpectButtonVisibility(
                    root,
                    "Approve Validated Save for Sync",
                    expectedVisible: true
                );
                break;
        }
    }

    private static void ExpectManualTransfersHidden(Node root)
    {
        var manualActions = new[]
        {
            "Upload Saves to Steam Cloud",
            "Pull Saves from Steam Cloud",
            "Get Steam Saves",
            "Upload to Steam",
            "Review Upload",
            "Confirm Upload",
        };
        foreach (var button in Descendants<Button>(root))
        {
            if (!button.IsVisibleInTree())
                continue;
            if (manualActions.Any(action => ButtonTextMatches(button, action)))
            {
                throw new InvalidOperationException(
                    $"Normal automatic-sync flow exposed manual action: {button.Text}"
                );
            }
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

    private static void ExpectButtonState(
        Node root,
        string expectedText,
        bool disabled
    )
    {
        foreach (var button in Descendants<Button>(root))
        {
            if (!button.IsVisibleInTree()
                || !ButtonTextMatches(button, expectedText))
            {
                continue;
            }

            if (button.Disabled != disabled)
            {
                throw new InvalidOperationException(
                    $"Button '{expectedText}' disabled={button.Disabled}; expected {disabled}."
                );
            }
            return;
        }

        throw new InvalidOperationException(
            $"Visible button was not found: {expectedText}"
        );
    }

    private static bool ButtonTextMatches(Button button, string expectedText)
        => button.Text.Equals(
            expectedText,
            StringComparison.OrdinalIgnoreCase
        ) || button.Text.StartsWith(
            expectedText + "\n",
            StringComparison.OrdinalIgnoreCase
        ) || button.AccessibilityName.Equals(
            expectedText,
            StringComparison.OrdinalIgnoreCase
        );

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
