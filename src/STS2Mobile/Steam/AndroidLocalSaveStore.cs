using System;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore : ISaveStore
{
    private readonly string _basePath;
    private readonly string _basePathWithSeparator;
    private readonly Action<string> _mutationCommitted;

    internal string RootPath => _basePath;

    internal AndroidLocalSaveStore()
        : this(OS.GetUserDataDir(), mutationCommitted: null)
    {
    }

    internal AndroidLocalSaveStore(string basePath)
        : this(basePath, mutationCommitted: null)
    {
    }

    internal AndroidLocalSaveStore(Action<string> mutationCommitted)
        : this(OS.GetUserDataDir(), mutationCommitted)
    {
    }

    internal AndroidLocalSaveStore(
        string basePath,
        Action<string> mutationCommitted
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        _basePath = Path.GetFullPath(basePath);
        _basePathWithSeparator = _basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? _basePath
            : _basePath + Path.DirectorySeparatorChar;
        _mutationCommitted = mutationCommitted;
        Directory.CreateDirectory(_basePath);
        PatchHelper.Log($"[Save] Android local save base: {_basePath}");
    }

    private void NotifyMutationCommitted(string path)
    {
        if (_mutationCommitted == null)
            return;

        try
        {
            _mutationCommitted(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Local save notification failed: {ex.GetType().Name}"
            );
        }
    }
}
