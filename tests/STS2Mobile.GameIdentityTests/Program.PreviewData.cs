using System;
using System.IO;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private sealed class TestPreviewData : IDisposable
    {
        private const string PreviewVariable = "STS2_LAUNCHER_PREVIEW";
        private readonly string? _previousPreview = Environment.GetEnvironmentVariable(PreviewVariable);
        private readonly string? _previousData = Environment.GetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable);
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "sts2-managed-tests", Guid.NewGuid().ToString("N"));

        internal TestPreviewData()
        {
            Directory.CreateDirectory(_directory);
            // Godot's managed metadata is available to these tests, but its native
            // engine is absent. Keep AppPaths on its real preview-data path.
            Environment.SetEnvironmentVariable(PreviewVariable, "1");
            Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, _directory);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(PreviewVariable, _previousPreview);
            Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, _previousData);
            Directory.Delete(_directory, recursive: true);
        }
    }
}
