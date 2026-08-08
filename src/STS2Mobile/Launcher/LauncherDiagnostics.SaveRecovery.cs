#nullable enable

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal static string WriteSaveRecoveryBundle(
        string json,
        string fallbackDirectory
    )
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException("Recovery export bundle is empty");
        var bundleBytes = Encoding.UTF8.GetBytes(json);
        var identity = ReadSaveRecoveryExportIdentity(bundleBytes);
        var fileName =
            $"sts2-save-recovery-bundle-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
        var targetPath = TryGetExternalDiagnosticsPath(fileName)
            ?? Path.Combine(fallbackDirectory, fileName);
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        var stagingPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllBytes(stagingPath, bundleBytes);
            File.Move(stagingPath, targetPath, overwrite: true);
            if (!File.Exists(targetPath))
                throw new IOException("Recovery export bundle is missing after write");
            var readBackBytes = File.ReadAllBytes(targetPath);
            if (!readBackBytes.AsSpan().SequenceEqual(bundleBytes))
            {
                throw new IOException(
                    "Recovery export bundle failed read-back verification"
                );
            }
            var readBackIdentity = ReadSaveRecoveryExportIdentity(readBackBytes);
            if (readBackIdentity != identity)
            {
                throw new IOException(
                    "Recovery export identity changed during read-back verification"
                );
            }
            var bundleSha256 = AutomaticSyncHash.Compute(readBackBytes);
            PatchHelper.Log(
                "[Recovery] STS2_SAVE_EXPORT_COMPLETE "
                    + JsonSerializer.Serialize(new
                    {
                        Event = "save-recovery-export-complete",
                        Version = 1,
                        identity.ExportId,
                        BundleSha256 = bundleSha256,
                        identity.CurrentAndroidTreeSha256,
                        identity.SelectedSaveContextSha256,
                    })
            );
            return targetPath;
        }
        finally
        {
            if (File.Exists(stagingPath))
                File.Delete(stagingPath);
        }
    }

    private static SaveRecoveryExportIdentity ReadSaveRecoveryExportIdentity(
        byte[] bytes
    )
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var identity = new SaveRecoveryExportIdentity(
                root.GetProperty("ExportId").GetString() ?? "",
                root.GetProperty("CurrentAndroidTreeSha256").GetString() ?? "",
                root.GetProperty("SelectedSaveContextSha256").GetString() ?? ""
            );
            if (!IsLowerHex(identity.ExportId, 32)
                || !IsLowerHex(identity.CurrentAndroidTreeSha256, 64)
                || !IsLowerHex(identity.SelectedSaveContextSha256, 64))
            {
                throw new InvalidDataException(
                    "Recovery export bundle has an invalid evidence identity"
                );
            }
            return identity;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                "Recovery export bundle is not valid JSON",
                ex
            );
        }
    }

    private static bool IsLowerHex(string value, int length)
    {
        if (value.Length != length)
            return false;
        foreach (var character in value)
        {
            if (character is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f'))
            {
                return false;
            }
        }
        return true;
    }

    private readonly record struct SaveRecoveryExportIdentity(
        string ExportId,
        string CurrentAndroidTreeSha256,
        string SelectedSaveContextSha256
    );
}
