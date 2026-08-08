using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class ShaderWarmupMaterialScanner
    {
        private static void VisitFiles(
            string dirPath,
            Action<string, string> visitFile,
            LauncherMonotonicDeadline deadline,
            ShaderWarmupMaterialScanDiagnostics diagnostics
        )
        {
            try
            {
                if (deadline.IsExpired)
                {
                    diagnostics.MarkDeadlineReached();
                    return;
                }

                using var dir = DirAccess.Open(dirPath);
                if (dir == null)
                    return;

                dir.ListDirBegin();
                string fileName;
                while ((fileName = dir.GetNext()) != "")
                {
                    if (deadline.IsExpired)
                    {
                        diagnostics.MarkDeadlineReached();
                        break;
                    }

                    if (ShouldSkip(fileName))
                        continue;

                    if (dir.CurrentIsDir())
                    {
                        VisitFiles(
                            ChildPath(dirPath, fileName),
                            visitFile,
                            deadline,
                            diagnostics
                        );
                        continue;
                    }

                    visitFile(dirPath, fileName);
                }
                dir.ListDirEnd();
            }
            catch (Exception ex)
            {
                diagnostics.RecordDirectoryEnumerationFailure(dirPath, ex);
            }
        }

        private static string ChildPath(string dirPath, string fileName)
            => $"{dirPath}/{fileName}";

        private static bool ShouldSkip(string fileName)
            => fileName is "." or ".." or "debug";
    }
}
