using System;
using System.Collections.Generic;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void AssetPreloadBudgetPreservesQueuedWork()
    {
        var pending = new Queue<int>();
        for (var i = 0; i < 100; i++) pending.Enqueue(i);
        var loaded = new List<int>();
        var frames = 0;
        while (pending.Count > 0)
        {
            var before = loaded.Count;
            var budget = new AssetPreloadFrameBudget(4, 4, () => 0);
            while (pending.Count > 0 && budget.TryBeginItem()) loaded.Add(pending.Dequeue());
            True(loaded.Count - before <= 4, "A frame cannot drain the entire resource backlog.");
            frames++;
        }
        Equal(25, frames, "All assets must eventually finish across bounded frames.");
        for (var i = 0; i < 100; i++) Equal(i, loaded[i], "FIFO loading order must survive yielding.");
    }

    private static void AssetPreloadBudgetYieldsAfterExpensiveItem()
    {
        double elapsed = 0;
        var budget = new AssetPreloadFrameBudget(8, 4, () => elapsed);
        True(budget.TryBeginItem(), "Every frame must make progress.");
        elapsed = 5;
        True(!budget.TryBeginItem(), "An expensive resource must yield before loading another.");
        var nextFrame = new AssetPreloadFrameBudget(8, 4, () => elapsed);
        True(nextFrame.TryBeginItem(), "The next frame gets a fresh budget.");
        elapsed += 4;
        True(!nextFrame.TryBeginItem(), "The time bound must be relative to each frame.");
    }
}
