using System;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore :
    ISaveStore,
    ICancellableSaveStore,
    IRawSaveStore,
    IRecoverySaveStore
{
    private const string VerboseDiagnosticsMarker = ".sts2_verbose_save_diagnostics";
    private readonly string _basePath;
    private readonly string _basePathWithSeparator;

    internal AndroidLocalSaveStore()
        : this(OS.GetUserDataDir())
    {
    }

    internal AndroidLocalSaveStore(string basePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        _basePath = Path.GetFullPath(basePath);
        _basePathWithSeparator = _basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? _basePath
            : _basePath + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_basePath);
        PatchHelper.Log($"[Save] Android local save base: {_basePath}");
    }

    private bool VerboseDiagnosticsEnabled
        => File.Exists(Path.Combine(_basePath, VerboseDiagnosticsMarker));
}
