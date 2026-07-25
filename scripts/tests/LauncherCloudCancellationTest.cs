using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudCancellationTest
{
    private static int _passed;

    private static async Task<int> Main()
    {
        await RunAsync("only one operation can own the session", OnlyOneOperationCanRunAsync);
        await RunAsync("active cancellation drains the worker", CancellationDrainsWorkerAsync);
        await RunAsync("timeout cancels before returning", TimeoutCancelsBeforeReturningAsync);
        await RunAsync("external cancellation is not reported as timeout", ExternalCancellationRemainsCancellationAsync);
        await RunAsync("disposal cancels and drains synchronously", DisposalCancelsAndDrainsAsync);
        await RunAsync("completed sessions permit the next operation", CompletedSessionCanBeReusedAsync);
        await RunAsync("save-store tokens reach cancellable I/O", SaveStoreTokenIsPropagatedAsync);
        await RunAsync("legacy save-store cancellation waits for drain", LegacySaveStoreIsDrainedAsync);
        await RunAsync("atomic write replaces only after success", AtomicWriteReplacesAfterSuccessAsync);
        await RunAsync("cancelled atomic write preserves destination", CancelledAtomicWritePreservesDestinationAsync);
        await RunAsync("timed-out atomic write cannot mutate after return", TimedOutAtomicWriteCannotMutateAfterReturnAsync);

        Console.WriteLine($"Launcher cloud cancellation tests passed {_passed}/11.");
        return 0;
    }

    private static async Task RunAsync(string name, Func<Task> test)
    {
        await test();
        _passed++;
        Console.WriteLine($"[PASS] {name}");
    }

    private static async Task OnlyOneOperationCanRunAsync()
    {
        using var manager = new CloudOperationSessionManager();
        var entered = NewCompletionSource<bool>();
        var release = NewCompletionSource<bool>();
        var calls = 0;

        var firstAccepted = manager.TryRun(
            async cancellationToken =>
            {
                Interlocked.Increment(ref calls);
                entered.TrySetResult(true);
                await release.Task.WaitAsync(cancellationToken);
            },
            out var first
        );
        await entered.Task;
        var secondAccepted = manager.TryRun(
            _ =>
            {
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            },
            out var rejected
        );

        Expect(firstAccepted, "The first operation was rejected.");
        Expect(!secondAccepted, "A concurrent operation was accepted.");
        Expect(ReferenceEquals(rejected, Task.CompletedTask), "Rejected operation returned a live task.");
        Expect(calls == 1, $"Expected one operation invocation, observed {calls}.");

        release.TrySetResult(true);
        await first;
    }

    private static async Task CancellationDrainsWorkerAsync()
    {
        using var manager = new CloudOperationSessionManager();
        var entered = NewCompletionSource<bool>();
        var cancellationObserved = false;
        var finallyCompleted = false;

        Expect(
            manager.TryRun(
                async cancellationToken =>
                {
                    entered.TrySetResult(true);
                    try
                    {
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        cancellationObserved = true;
                        throw;
                    }
                    finally
                    {
                        await Task.Delay(15);
                        finallyCompleted = true;
                    }
                },
                out var execution
            ),
            "The operation was rejected."
        );

        await entered.Task;
        await manager.CancelAndDrainAsync();
        await ExpectCanceledAsync(execution);

        Expect(cancellationObserved, "The worker did not observe cancellation.");
        Expect(finallyCompleted, "Cancellation returned before worker cleanup.");
        Expect(!manager.IsActive, "The manager remained active after draining.");
    }

    private static async Task TimeoutCancelsBeforeReturningAsync()
    {
        var cancellationObserved = false;
        var cleanupCompleted = false;
        var lateMutation = false;
        var stopwatch = Stopwatch.StartNew();

        await ExpectTimeoutAsync(
            LauncherTimeout.RunOrThrowAsync(
                async cancellationToken =>
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                        lateMutation = true;
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        cancellationObserved = true;
                        throw;
                    }
                    finally
                    {
                        await Task.Delay(20);
                        cleanupCompleted = true;
                    }

                    return "late result";
                },
                CancellationToken.None,
                timeoutMs: 25,
                "Synthetic Pull timed out"
            )
        );
        stopwatch.Stop();

        Expect(cancellationObserved, "Timeout did not reach the worker.");
        Expect(cleanupCompleted, "Timeout returned before worker cleanup.");
        Expect(!lateMutation, "Worker mutated state after timeout.");
        Expect(stopwatch.ElapsedMilliseconds >= 40, "Timeout did not drain asynchronous cleanup.");
    }

    private static async Task ExternalCancellationRemainsCancellationAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var entered = NewCompletionSource<bool>();
        var operation = LauncherTimeout.RunOrThrowAsync(
            async cancellationToken =>
            {
                entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return "";
            },
            cancellation.Token,
            timeoutMs: 2_000,
            "Should not time out"
        );

        await entered.Task;
        cancellation.Cancel();
        await ExpectCanceledAsync(operation);
    }

    private static async Task DisposalCancelsAndDrainsAsync()
    {
        var manager = new CloudOperationSessionManager();
        var entered = NewCompletionSource<bool>();
        var cleanupCompleted = false;
        Expect(
            manager.TryRun(
                async cancellationToken =>
                {
                    try
                    {
                        entered.TrySetResult(true);
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                    }
                    finally
                    {
                        await Task.Delay(15);
                        cleanupCompleted = true;
                    }
                },
                out var execution
            ),
            "The operation was rejected."
        );

        await entered.Task;
        manager.Dispose();
        await ExpectCanceledAsync(execution);

        Expect(cleanupCompleted, "Dispose returned before cleanup completed.");
        Expect(!manager.IsActive, "Disposed manager retained an active operation.");
        Expect(
            !manager.TryRun(_ => Task.CompletedTask, out _),
            "Disposed manager accepted an operation."
        );
    }

    private static async Task CompletedSessionCanBeReusedAsync()
    {
        using var manager = new CloudOperationSessionManager();
        Expect(
            manager.TryRun(_ => Task.CompletedTask, out var first),
            "The first operation was rejected."
        );
        await first;

        Expect(
            manager.TryRun(_ => Task.CompletedTask, out var second),
            "The second operation was rejected after completion."
        );
        await second;
    }

    private static async Task SaveStoreTokenIsPropagatedAsync()
    {
        var store = new TokenAwareStore();
        using var cancellation = new CancellationTokenSource();
        var read = CancellableSaveStore.ReadFileAsync(
            store,
            "profile1/saves/progress.save",
            cancellation.Token
        );

        await store.ReadEntered.Task;
        cancellation.Cancel();
        await ExpectCanceledAsync(read);
        Expect(
            store.ObservedReadToken == cancellation.Token,
            "Read did not receive the operation token."
        );

        using var writeCancellation = new CancellationTokenSource();
        writeCancellation.Cancel();
        await ExpectCanceledAsync(
            CancellableSaveStore.WriteFileAsync(
                store,
                "profile1/saves/progress.save",
                "{}",
                writeCancellation.Token
            )
        );
        Expect(
            store.ObservedWriteToken == writeCancellation.Token,
            "Write did not receive the operation token."
        );
    }

    private static async Task LegacySaveStoreIsDrainedAsync()
    {
        var store = new LegacyStore();
        using var cancellation = new CancellationTokenSource();
        var read = CancellableSaveStore.ReadFileAsync(
            store,
            "profile1/saves/progress.save",
            cancellation.Token
        );

        await store.ReadEntered.Task;
        cancellation.Cancel();
        await Task.Delay(20);
        Expect(
            !read.IsCompleted,
            "Legacy read returned while its underlying work was still pending."
        );

        store.ReleaseRead.TrySetResult("{}");
        await ExpectCanceledAsync(read);
        Expect(store.ReadCompleted, "Legacy read did not drain before cancellation returned.");
    }

    private static async Task AtomicWriteReplacesAfterSuccessAsync()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var destination = Path.Combine(directory, "progress.save");
            await File.WriteAllTextAsync(destination, "old");
            await CancellableAtomicFile.WriteAllTextAsync(
                destination,
                "new",
                overwrite: true,
                CancellationToken.None
            );

            Expect(
                await File.ReadAllTextAsync(destination) == "new",
                "Successful atomic write did not replace the destination."
            );
            Expect(
                Directory.GetFiles(directory, "*.sts2-cloud-*.tmp").Length == 0,
                "Successful atomic write left a staging file."
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task CancelledAtomicWritePreservesDestinationAsync()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var destination = Path.Combine(directory, "progress.save");
            await File.WriteAllTextAsync(destination, "old");
            using var cancellation = new CancellationTokenSource();
            var entered = NewCompletionSource<bool>();
            var write = CancellableAtomicFile.WriteAsync(
                destination,
                overwrite: true,
                async (stagingPath, token) =>
                {
                    await File.WriteAllTextAsync(stagingPath, "partial");
                    entered.TrySetResult(true);
                    await Task.Delay(Timeout.Infinite, token);
                },
                cancellation.Token
            );

            await entered.Task;
            cancellation.Cancel();
            await ExpectCanceledAsync(write);

            Expect(
                await File.ReadAllTextAsync(destination) == "old",
                "Cancelled atomic write damaged the prior destination."
            );
            Expect(
                Directory.GetFiles(directory, "*.sts2-cloud-*.tmp").Length == 0,
                "Cancelled atomic write left a staging file."
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task TimedOutAtomicWriteCannotMutateAfterReturnAsync()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var destination = Path.Combine(directory, "progress.save");
            await File.WriteAllTextAsync(destination, "old");

            await ExpectTimeoutAsync(
                LauncherTimeout.RunOrThrowAsync(
                    async cancellationToken =>
                    {
                        await CancellableAtomicFile.WriteAsync(
                            destination,
                            overwrite: true,
                            async (stagingPath, token) =>
                            {
                                await File.WriteAllTextAsync(
                                    stagingPath,
                                    "partial",
                                    token
                                );
                                await Task.Delay(
                                    Timeout.Infinite,
                                    token
                                );
                            },
                            cancellationToken
                        );
                        return "late success";
                    },
                    CancellationToken.None,
                    timeoutMs: 25,
                    "Synthetic save write timed out"
                )
            );

            Expect(
                await File.ReadAllTextAsync(destination) == "old",
                "Timed-out write mutated the destination before returning."
            );
            Expect(
                Directory.GetFiles(
                    directory,
                    "*.sts2-cloud-*.tmp"
                ).Length == 0,
                "Timed-out write returned before staging cleanup."
            );

            await Task.Delay(75);
            Expect(
                await File.ReadAllTextAsync(destination) == "old",
                "Timed-out write mutated the destination after returning."
            );
            Expect(
                Directory.GetFiles(
                    directory,
                    "*.sts2-cloud-*.tmp"
                ).Length == 0,
                "Timed-out write recreated staging state after returning."
            );
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task ExpectCanceledAsync(Task task)
    {
        try
        {
            await task;
            throw new InvalidOperationException("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task ExpectTimeoutAsync(Task task)
    {
        try
        {
            await task;
            throw new InvalidOperationException("Expected timeout.");
        }
        catch (TimeoutException)
        {
        }
    }

    private static TaskCompletionSource<T> NewCompletionSource<T>()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"sts2-cloud-cancellation-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class TokenAwareStore :
        ISaveStore,
        ICancellableSaveStore
    {
        internal TaskCompletionSource<bool> ReadEntered { get; }
            = NewCompletionSource<bool>();

        internal CancellationToken ObservedReadToken { get; private set; }
        internal CancellationToken ObservedWriteToken { get; private set; }

        Task<string> ISaveStore.ReadFileAsync(string path)
            => Task.FromResult("");

        Task ISaveStore.WriteFileAsync(string path, string content)
            => Task.CompletedTask;

        async Task<string> ICancellableSaveStore.ReadFileAsync(
            string path,
            CancellationToken cancellationToken
        )
        {
            ObservedReadToken = cancellationToken;
            ReadEntered.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return "";
        }

        Task ICancellableSaveStore.WriteFileAsync(
            string path,
            string content,
            CancellationToken cancellationToken
        )
        {
            ObservedWriteToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class LegacyStore : ISaveStore
    {
        internal TaskCompletionSource<bool> ReadEntered { get; }
            = NewCompletionSource<bool>();

        internal TaskCompletionSource<string> ReleaseRead { get; }
            = NewCompletionSource<string>();

        internal bool ReadCompleted { get; private set; }

        async Task<string> ISaveStore.ReadFileAsync(string path)
        {
            ReadEntered.TrySetResult(true);
            var result = await ReleaseRead.Task;
            ReadCompleted = true;
            return result;
        }

        Task ISaveStore.WriteFileAsync(string path, string content)
            => Task.CompletedTask;
    }
}
