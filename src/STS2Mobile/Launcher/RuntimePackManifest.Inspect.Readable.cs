using System.Collections.Generic;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private static RuntimePackManifest InspectReadable(
        RuntimePackManifestInspectionContext context
    )
    {
        using var document = JsonDocument.Parse(File.ReadAllText(context.ManifestPath));
        var root = document.RootElement;
        var manifest = ReadManifest(context, root);

        return manifest.WithStatus(RuntimePackStatus(manifest));
    }

    private static RuntimePackManifest ReadManifest(
        RuntimePackManifestInspectionContext context,
        JsonElement root
    )
    {
        var declaredAndroidAssemblySha256 = ReadString(
            root,
            "androidAssemblySha256",
            "android_assembly_sha256",
            "sts2DllSha256",
            "sts2_dll_sha256"
        );
        return new RuntimePackManifest(
            context.ManifestPath,
            context.ExpectedBranch,
            context.ExpectedGameIdentity,
            ReadString(root, "packId", "pack_id", "id"),
            ReadString(root, "sourceBranch", "source_branch", "branch"),
            ReadString(root, "installGeneration", "install_generation"),
            ReadString(root, "sourcePckSha256", "source_pck_sha256", "pckSha256", "pck_sha256"),
            ReadString(root, "sourceAssemblySha256", "source_assembly_sha256", "desktopAssemblySha256", "desktop_assembly_sha256"),
            ReadString(root, "gameIdentityId", "game_identity_id"),
            declaredAndroidAssemblySha256,
            ReadString(root, "patchSetVersion", "patch_set_version", "patchVersion", "patch_version"),
            ReadString(root, "patchValidationStatus", "patch_validation_status", "patchStatus", "patch_status"),
            ReadString(root, "patchValidationReport", "patch_validation_report", "patchReport", "patch_report"),
            ReadString(root, "validationMode", "validation_mode"),
            ReadString(root, "validationSurfaceVersion", "validation_surface_version"),
            ReadStringArray(root, "supportAssemblies", "support_assemblies"),
            ReadStringDictionary(root, "supportAssemblySha256", "support_assembly_sha256"),
            HasProperty(root, "supportAssemblies", "support_assemblies"),
            HasProperty(root, "supportAssemblySha256", "support_assembly_sha256"),
            ReadInt(root, "checkedSymbolCount", "checked_symbol_count"),
            ReadInt(root, "presentSymbolCount", "present_symbol_count"),
            ReadInt(root, "missingSymbolCount", "missing_symbol_count"),
            ReadString(root, "minimumLauncherVersion", "minimum_launcher_version", "minLauncherVersion"),
            ReadBool(root, "generatedFromCleanDirectory", "generated_from_clean_directory"),
            "pending validation",
            exists: true,
            readable: true,
            context.AndroidAssemblyExists,
            context.AndroidAssemblyPath,
            context.AndroidAssemblyExists ? Sha256OrMissing(context.AndroidAssemblyPath) : "<missing>"
        );
    }

    private static string Sha256OrMissing(string path)
    {
        try
        {
            byte[] hash;
            if (OperatingSystem.IsAndroid())
            {
                hash = AndroidJavaCrypto.Sha256FileHashData(path);
            }
            else
            {
                using var stream = File.OpenRead(path);
                hash = SHA256.HashData(stream);
            }

            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
        catch
        {
            return "<hash failed>";
        }
    }
}
