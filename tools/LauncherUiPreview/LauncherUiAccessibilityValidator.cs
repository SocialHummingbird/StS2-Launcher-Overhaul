using System;
using System.Collections.Generic;
using Godot;

namespace LauncherUiPreview;

internal static class LauncherUiAccessibilityValidator
{
    private const float TouchTargetMinimum = 48f;
    private const float DesktopTargetMinimum = 40f;
    private const float BoundsTolerance = 1f;

    internal static void Validate(Control root, bool touchOptimized, Vector2 viewportSize)
    {
        var minimumHeight = touchOptimized ? TouchTargetMinimum : DesktopTargetMinimum;
        foreach (var button in Descendants<BaseButton>(root))
        {
            if (!button.IsVisibleInTree()
                || !IsFullyExposedByScrollAncestors(button))
                continue;

            var name = AccessibleName(button);
            Expect(name.Length > 0, $"Visible button has no accessible name: {button.GetPath()}");
            Expect(
                button.Size.Y + BoundsTolerance >= minimumHeight,
                $"Button '{name}' is {button.Size.Y:0.0}px high; expected at least {minimumHeight:0}px."
            );
            if (!button.Disabled)
            {
                Expect(
                    button.FocusMode != Control.FocusModeEnum.None,
                    $"Button '{name}' cannot receive keyboard/controller focus."
                );
            }
            ExpectInsideViewport(button, name, viewportSize);
        }

        foreach (var input in Descendants<LineEdit>(root))
        {
            if (!input.IsVisibleInTree()
                || !IsFullyExposedByScrollAncestors(input))
                continue;

            var name = AccessibleName(input);
            Expect(name.Length > 0, $"Visible input has no accessible name: {input.GetPath()}");
            Expect(
                input.Size.Y + BoundsTolerance >= minimumHeight,
                $"Input '{name}' is {input.Size.Y:0.0}px high; expected at least {minimumHeight:0}px."
            );
            Expect(
                input.FocusMode != Control.FocusModeEnum.None,
                $"Input '{name}' cannot receive keyboard/controller focus."
            );
            ExpectInsideViewport(input, name, viewportSize);
        }
    }

    private static string AccessibleName(Control control)
    {
        if (!string.IsNullOrWhiteSpace(control.AccessibilityName))
            return control.AccessibilityName.Trim();
        if (control is Button button && !string.IsNullOrWhiteSpace(button.Text))
            return button.Text.Trim();
        return control.TooltipText?.Trim() ?? "";
    }

    private static void ExpectInsideViewport(Control control, string name, Vector2 viewportSize)
    {
        var rect = control.GetGlobalRect();
        Expect(
            rect.Position.X >= -BoundsTolerance
                && rect.Position.Y >= -BoundsTolerance
                && rect.End.X <= viewportSize.X + BoundsTolerance
                && rect.End.Y <= viewportSize.Y + BoundsTolerance,
            $"Control '{name}' is outside viewport {viewportSize}: {rect}."
        );
    }

    private static bool IsFullyExposedByScrollAncestors(Control control)
    {
        var rect = control.GetGlobalRect();
        for (var ancestor = control.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is not ScrollContainer scroll || !scroll.IsVisibleInTree())
                continue;

            var viewport = scroll.GetGlobalRect();
            if (rect.Position.X < viewport.Position.X - BoundsTolerance
                || rect.Position.Y < viewport.Position.Y - BoundsTolerance
                || rect.End.X > viewport.End.X + BoundsTolerance
                || rect.End.Y > viewport.End.Y + BoundsTolerance)
            {
                return false;
            }
        }

        return true;
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

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
