using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal enum InstalledGameVersionReadiness
{
    Ready,
    AutomaticRepair,
    RedownloadRequired,
}

internal static partial class LauncherGameFiles
{
    private const uint PckHeaderBytes = 104;
    private const uint MaximumPckPathBytes = 4096;
    private const uint MaximumPckFileCount = 2_000_000;
    private const long MaximumManagedPckEntryBytes = 8L * 1024L * 1024L;
    private const long MaximumBranchMarkerBytes = 1024L * 1024L;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static InstalledGameVersionReadiness ClassifyInstalledVersion(
        string dataDir,
        string branch
    )
    {
        try
        {
            var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
            var state = BranchInstallStateStore.Current.Read(dataDir, normalizedBranch);
            if (!state.IsReady)
                return InstalledGameVersionReadiness.RedownloadRequired;

            var classification = state.PckPreparationVersion switch
            {
                AndroidPckPreparationVersions.V2 => InstalledGameVersionReadiness.Ready,
                AndroidPckPreparationVersions.V1 => InstalledGameVersionReadiness.AutomaticRepair,
                _ => InstalledGameVersionReadiness.RedownloadRequired,
            };
            if (classification == InstalledGameVersionReadiness.RedownloadRequired)
                return classification;

            if (!HasMatchingSteamProvenance(dataDir, normalizedBranch, state))
                return InstalledGameVersionReadiness.RedownloadRequired;

            if (!HasRecognizedManagedFmodEntries(
                    SteamGameInstallPaths.GameDirectory(dataDir, normalizedBranch)
                ))
            {
                return InstalledGameVersionReadiness.RedownloadRequired;
            }

            var installedIdentity = GameIdentityReader.ReadInstalledReadOnly(
                dataDir,
                normalizedBranch
            );
            if (!state.MatchesReadyIdentity(installedIdentity))
                return InstalledGameVersionReadiness.RedownloadRequired;

            return classification;
        }
        catch
        {
            return InstalledGameVersionReadiness.RedownloadRequired;
        }
    }

    internal static bool CanResumeAutomaticPckRepair(
        string dataDir,
        string branch,
        BranchInstallState state
    )
    {
        try
        {
            var normalizedBranch = SteamGameBranch.StorageIdentity(branch);
            if (state?.Status != BranchInstallStatus.Updating
                || !string.Equals(state.Branch, normalizedBranch, StringComparison.Ordinal)
                || !HasMatchingSteamProvenance(dataDir, normalizedBranch, state)
                || !HasRecognizedManagedFmodEntries(
                    SteamGameInstallPaths.GameDirectory(dataDir, normalizedBranch),
                    requireArm64V2: false
                ))
            {
                return false;
            }

            _ = GameIdentityReader.ResolveInstalledSourceAssemblyPath(
                SteamGameInstallPaths.GameDirectory(dataDir, normalizedBranch)
            );
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static bool HasArm64V2PckPostconditions(
        string dataDir,
        string branch
    )
    {
        try
        {
            return HasRecognizedManagedFmodEntries(
                SteamGameInstallPaths.GameDirectory(
                    dataDir,
                    SteamGameBranch.StorageIdentity(branch)
                ),
                requireArm64V2: true
            );
        }
        catch
        {
            return false;
        }
    }

    private static bool HasMatchingSteamProvenance(
        string dataDir,
        string branch,
        BranchInstallState state
    )
    {
        var markerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        var marker = new FileInfo(markerPath);
        marker.Refresh();
        if (!marker.Exists || marker.Length < 1 || marker.Length > MaximumBranchMarkerBytes)
            return false;

        var lines = File.ReadAllLines(markerPath, StrictUtf8);
        if (!TryReadSingleMarkerValue(
                lines,
                LauncherBranchMarkerFields.Branch,
                out var markerBranch
            )
            || !string.Equals(
                SteamGameBranch.StorageIdentity(markerBranch),
                branch,
                StringComparison.Ordinal
            )
            || !TryReadSingleMarkerValue(
                lines,
                LauncherBranchMarkerFields.InstallSlotKind,
                out var slotKind
            )
            || !string.Equals(
                slotKind,
                SteamGameInstallPaths.VersionSlotKind(branch),
                StringComparison.Ordinal
            )
            || !TryReadSingleMarkerValue(
                lines,
                LauncherBranchMarkerFields.InstallSlotDirectory,
                out var slotDirectory
            )
            || !LauncherAndroidAppPrivatePath.MarkerPathMatchesExpectedPath(
                slotDirectory,
                SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
                dataDir
            )
            || !TryReadSingleMarkerValue(
                lines,
                LauncherBranchMarkerFields.DepotManifestCount,
                out var depotCountText
            )
            || !int.TryParse(
                depotCountText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var depotCount
            )
            || depotCount < 1
            || depotCount != state.Depots.Count)
        {
            return false;
        }

        var rows = lines
            .Where(line => line.StartsWith(
                LauncherBranchMarkerFields.DepotManifestRow,
                StringComparison.OrdinalIgnoreCase
            ))
            .ToArray();
        if (rows.Length != depotCount)
            return false;

        var matchedDepots = new HashSet<ulong>();
        foreach (var row in rows)
        {
            if (!TryParseDepotProvenance(row, out var depotId, out var manifestId, out var rowBranch, out var manifestSource)
                || !matchedDepots.Add(depotId)
                || !string.Equals(
                    SteamGameBranch.StorageIdentity(rowBranch),
                    branch,
                    StringComparison.Ordinal
                ))
            {
                return false;
            }

            var expected = state.Depots.SingleOrDefault(depot => depot.DepotId == depotId);
            if (expected == null
                || expected.ManifestId != manifestId
                || !string.Equals(
                    expected.ManifestSource,
                    manifestSource,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return false;
            }
        }

        return matchedDepots.Count == state.Depots.Count;
    }

    private static bool TryReadSingleMarkerValue(
        IEnumerable<string> lines,
        string prefix,
        out string value
    )
    {
        value = string.Empty;
        var found = false;
        foreach (var line in lines)
        {
            if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            if (found)
                return false;

            value = line.Substring(prefix.Length).Trim();
            found = true;
        }

        return found && !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryParseDepotProvenance(
        string row,
        out ulong depotId,
        out ulong manifestId,
        out string branch,
        out string manifestSource
    )
    {
        depotId = 0;
        manifestId = 0;
        branch = string.Empty;
        manifestSource = string.Empty;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var payload = row.Substring(LauncherBranchMarkerFields.DepotManifestRow.Length);
        foreach (var token in payload.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = token.IndexOf('=');
            if (separator <= 0
                || separator == token.Length - 1
                || !fields.TryAdd(token.Substring(0, separator), token.Substring(separator + 1)))
            {
                return false;
            }
        }

        return fields.TryGetValue("depot", out var depotText)
            && ulong.TryParse(
                depotText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out depotId
            )
            && depotId > 0
            && fields.TryGetValue("manifest", out var manifestText)
            && ulong.TryParse(
                manifestText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out manifestId
            )
            && manifestId > 0
            && fields.TryGetValue("branch", out branch)
            && !string.IsNullOrWhiteSpace(branch)
            && fields.TryGetValue("manifestSource", out manifestSource)
            && !string.IsNullOrWhiteSpace(manifestSource);
    }

    private static bool HasRecognizedManagedFmodEntries(
        string gameDirectory,
        bool requireArm64V2 = false
    )
    {
        var pckPath = Path.Combine(gameDirectory, LauncherStorageNames.GamePck);
        using var stream = new FileStream(
            pckPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.SequentialScan
        );
        using var reader = new BinaryReader(stream, StrictUtf8, leaveOpen: true);
        if (stream.Length < PckHeaderBytes || reader.ReadUInt32() != PckMagic)
            return false;

        reader.ReadUInt32(); // format version
        reader.ReadUInt32(); // major
        reader.ReadUInt32(); // minor
        reader.ReadUInt32(); // patch
        var headerFlags = reader.ReadUInt32();
        var fileBase = reader.ReadInt64();
        var directoryBase = reader.ReadInt64();
        if (directoryBase < PckHeaderBytes || directoryBase > stream.Length - sizeof(uint))
            return false;

        stream.Position = directoryBase;
        var fileCount = reader.ReadUInt32();
        if (fileCount < 1 || fileCount > MaximumPckFileCount)
            return false;

        var relativeOffsets = (headerFlags & 0x02) != 0;
        var managedEntries = new Dictionary<string, PckEntryLocation>(StringComparer.Ordinal);
        for (uint index = 0; index < fileCount; index++)
        {
            if (stream.Position > stream.Length - sizeof(uint))
                return false;

            var pathLength = reader.ReadUInt32();
            if (pathLength < 1
                || pathLength > MaximumPckPathBytes
                || pathLength > stream.Length - stream.Position
                || stream.Position + pathLength > stream.Length - 36)
            {
                return false;
            }

            var pathBytes = reader.ReadBytes((int)pathLength);
            if (pathBytes.Length != pathLength)
                return false;
            var path = StrictUtf8.GetString(pathBytes).TrimEnd('\0');
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var offset = reader.ReadInt64();
            var size = reader.ReadInt64();
            if (reader.ReadBytes(16).Length != 16)
                return false;
            reader.ReadUInt32(); // entry flags

            if (!TryResolvePckEntryBounds(
                    stream.Length,
                    relativeOffsets,
                    fileBase,
                    offset,
                    size,
                    out var absoluteOffset
                ))
            {
                return false;
            }

            var managedPath = ManagedPckPath(path);
            if (managedPath == null)
                continue;
            if (size > MaximumManagedPckEntryBytes
                || !managedEntries.TryAdd(
                    managedPath,
                    new PckEntryLocation(absoluteOffset, (int)size)
                ))
            {
                return false;
            }
        }

        if (managedEntries.Count != 4)
            return false;

        var projectGodot = ReadPckEntry(
            stream,
            managedEntries[ManagedFmodPckForms.ProjectGodotPath]
        );
        if (!HasExpectedPair(
                projectGodot,
                ManagedFmodPckForms.ProjectGodotSetting,
                ManagedFmodPckForms.DisabledTextEntry(
                    ManagedFmodPckForms.ProjectGodotSetting
                ),
                requireArm64V2
            ))
        {
            return false;
        }

        var projectBinary = ReadPckEntry(
            stream,
            managedEntries[ManagedFmodPckForms.ProjectBinaryPath]
        );
        if (!HasExpectedPair(
                projectBinary,
                ManagedFmodPckForms.ProjectBinaryAutoload,
                ManagedFmodPckForms.DisabledProjectBinaryAutoload,
                requireArm64V2
            ))
        {
            return false;
        }

        var extensionList = ReadPckEntry(
            stream,
            managedEntries[ManagedFmodPckForms.ExtensionListPath]
        );
        if (!Contains(extensionList, ManagedFmodPckForms.ExtensionListEntry))
            return false;

        var gameScene = ReadPckEntry(
            stream,
            managedEntries[ManagedFmodPckForms.GameScenePath]
        );
        return ManagedFmodPckForms.GameSceneEntries.All(entry =>
            HasExpectedPair(
                gameScene,
                entry,
                ManagedFmodPckForms.DisabledTextEntry(entry),
                requireArm64V2
            )
        );
    }

    private static bool TryResolvePckEntryBounds(
        long fileLength,
        bool relativeOffsets,
        long fileBase,
        long offset,
        long size,
        out long absoluteOffset
    )
    {
        absoluteOffset = offset;
        if (relativeOffsets)
        {
            if (fileBase < 0 || offset < 0 || offset > long.MaxValue - fileBase)
                return false;
            absoluteOffset = fileBase + offset;
        }

        return absoluteOffset >= 0
            && size >= 0
            && absoluteOffset <= fileLength
            && size <= fileLength - absoluteOffset;
    }

    private static string ManagedPckPath(string path)
    {
        var normalized = path.StartsWith("res://", StringComparison.Ordinal)
            ? path.Substring(6)
            : path;
        return normalized == ManagedFmodPckForms.ProjectBinaryPath
            || normalized == ManagedFmodPckForms.ProjectGodotPath
            || normalized == ManagedFmodPckForms.ExtensionListPath
            || normalized == ManagedFmodPckForms.GameScenePath
                ? normalized
                : null;
    }

    private static byte[] ReadPckEntry(FileStream stream, PckEntryLocation entry)
    {
        stream.Position = entry.Offset;
        var content = new byte[entry.Size];
        stream.ReadExactly(content, 0, content.Length);
        return content;
    }

    private static bool HasExpectedPair(
        byte[] content,
        string first,
        string second,
        bool requireFirst
    )
    {
        var hasFirst = Contains(content, first);
        var hasSecond = Contains(content, second);
        return requireFirst
            ? hasFirst && !hasSecond
            : hasFirst != hasSecond;
    }

    private static bool Contains(byte[] content, string value)
    {
        var search = Encoding.UTF8.GetBytes(value);
        for (var index = 0; index <= content.Length - search.Length; index++)
        {
            var match = true;
            for (var offset = 0; offset < search.Length; offset++)
            {
                if (content[index + offset] == search[offset])
                    continue;
                match = false;
                break;
            }
            if (match)
                return true;
        }

        return false;
    }

    private readonly record struct PckEntryLocation(long Offset, int Size);
}
