using System;
using System.Collections.Generic;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    internal readonly partial struct BranchOption
    {
        internal string StatusText
        {
            get
            {
                var details = new List<string>();

                if (Source == "Steam app-info")
                {
                    if (PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Add("Download blocked: Steam marks this branch as password-protected, and beta password entry is not implemented yet.");
                        if (WindowsManifestDepotCount > 0)
                            details.Add($"Steam exposed {WindowsManifestDepotCount} Windows depot manifest(s), but the password gate still blocks this launcher from downloading it.");
                    }
                    else if (WindowsManifestDepotCount <= 0)
                    {
                        details.Add(MetadataVisible
                            ? "Download blocked: this branch is visible to this account, but no Windows depot manifest was exposed."
                            : "Download blocked: this branch was not listed in Steam branch metadata for this account.");
                    }
                    else
                    {
                        details.Add($"Downloadable for this account ({WindowsManifestDepotCount} Windows depot manifest(s)).");
                    }

                    if (PasswordRequired.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Add("Steam did not report a password requirement.");
                    }
                    else if (!PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                        details.Add("Steam did not expose password status.");

                    if (!string.IsNullOrWhiteSpace(BuildId))
                        details.Add($"Build ID: {BuildId}.");

                    if (!string.IsNullOrWhiteSpace(Description))
                        details.Add($"Description: {Description}.");
                }
                else if (!string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Source, "local install", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Add("Downloaded local install slot is available on this device.");
                        details.Add("Use Refresh Game Versions before redownloading or updating this branch.");
                    }
                    else
                    {
                        details.Add("Steam app-info metadata has not been captured for this option yet. Use Refresh Game Versions before downloading.");
                    }
                }
                else
                {
                    details.Add("Default/public branch remains available even before Steam branch metadata is refreshed.");
                }

                return string.Join(" ", details);
            }
        }
    }
}
