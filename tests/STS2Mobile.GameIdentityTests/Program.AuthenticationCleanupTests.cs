using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static SteamAuth AuthRetryFixture(ManualResetEventSlim gate)
    {
        // Exercise retry policy without constructing the Godot-only Java HTTP bridge.
        var auth = (SteamAuth)RuntimeHelpers.GetUninitializedObject(typeof(SteamAuth));
        typeof(SteamAuth).GetField("_connectedGate", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(auth, gate);
        return auth;
    }
    private static bool RunAuthRetry(SteamAuth auth, Action begin)
    {
        var method = typeof(SteamAuth).GetMethod("TryConnectWithRetriesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var arguments = new object[] { begin, "fixture start failed", "fixture retry" };
        return ((Task<bool>)method.Invoke(auth, arguments)!).GetAwaiter().GetResult();
    }

    private static void AuthRetryAcceptsFirstConnection()
    {
        using var gate = new ManualResetEventSlim(false);
        var auth = AuthRetryFixture(gate);
        var starts = 0;
        True(RunAuthRetry(auth, () => { starts++; gate.Set(); }), "An observed connection must complete the retry operation.");
        Equal(1, starts, "Connection success must not start another attempt.");
    }

    private static void AuthRetryBoundsStartFailures()
    {
        using var gate = new ManualResetEventSlim(false);
        var auth = AuthRetryFixture(gate);
        var starts = 0;
        var retries = 0;
        auth.LogMessage += message => { if (message == "fixture retry") retries++; };
        True(!RunAuthRetry(auth, () => { starts++; throw new InvalidOperationException("fixture failure"); }), "Repeated synchronous failures must settle as unavailable.");
        Equal(3, starts, "Desktop authentication retains its three-attempt limit.");
        Equal(2, retries, "Only intermediate failures schedule another attempt.");
    }

    private static void SessionCompletionRejectsStaleAttempt()
    {
        foreach (var staleResult in new string?[] { null, "stale failure" })
        {
            using var fixture = GameInstallFixture.Create();
            using var model = new LauncherModel(fixture.DataDir);
            var states = new List<LauncherModel.SessionState>();
            model.SessionStateChanged += states.Add;
            var method = typeof(LauncherModel).GetMethod("RunConnectionAttemptAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var old = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var current = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var oldTask = (Task)method.Invoke(model, new object[] { LauncherModel.SessionState.Connecting, new Func<int, Task<string?>>(_ => old.Task) })!;
            var currentTask = (Task)method.Invoke(model, new object[] { LauncherModel.SessionState.Authenticating, new Func<int, Task<string?>>(_ => current.Task) })!;
            states.Clear();
            old.SetResult(staleResult);
            oldTask.GetAwaiter().GetResult();
            Equal(0, states.Count, "Late success and failure must leave the replacement session alone.");
            current.SetResult(null);
            currentTask.GetAwaiter().GetResult();
            Equal(1, states.Count, "Only the current session result can publish completion.");
            Equal(LauncherModel.SessionState.LoggedIn, states[0], "The current successful session remains usable.");
        }
    }
}
