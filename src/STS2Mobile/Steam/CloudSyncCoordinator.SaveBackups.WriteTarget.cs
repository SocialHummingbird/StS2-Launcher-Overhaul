using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private readonly struct BackupWriteTarget
        {
            private readonly string _path;
            private readonly string _content;
            private readonly Action<string> _onWritten;

            internal BackupWriteTarget(
                string path,
                string content,
                Action<string> onWritten
            )
            {
                _path = path;
                _content = content;
                _onWritten = onWritten;
            }

            internal void Write()
            {
                File.WriteAllText(_path, _content);
                _onWritten(_path);
            }
        }
    }
}
