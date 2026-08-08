using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private async Task<string?> AdoptConnectionAfterVerificationAsync(
        SteamConnection connection,
        Func<SteamConnection, Task<string?>> verifyAsync,
        Func<Task<string?>> beforeAdoptAsync = null
    )
    {
        var adopted = false;
        try
        {
            var failure = await verifyAsync(connection);
            if (failure != null)
                return failure;

            if (beforeAdoptAsync != null)
            {
                var beforeAdoptFailure = await beforeAdoptAsync();
                if (beforeAdoptFailure != null)
                    return beforeAdoptFailure;
            }

            UseConnection(connection);
            adopted = true;
            return null;
        }
        finally
        {
            if (!adopted)
                connection.Dispose();
        }
    }

    private void DropConnection(SteamConnection connection)
    {
        if (ReferenceEquals(_connection, connection))
            _connection = null;

        connection.Dispose();
    }

    private SteamConnection CreateSavedCredentialConnection()
    {
        if (_credentialStore.TryCreateConnection(out var connection))
            return connection;

        throw new InvalidOperationException("No saved credentials");
    }
}
