using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed class SaveBatchBuffer
    {
        private readonly object _lock = new();
        private readonly List<(string canonPath, byte[] bytes)> _files = new();
        private bool _isCollecting;

        private void BeginCollecting()
        {
            lock (_lock)
            {
                _isCollecting = true;
                _files.Clear();
            }
        }

        private bool TryCollect(string canonPath, byte[] bytes)
        {
            lock (_lock)
            {
                if (!_isCollecting)
                    return false;

                _files.Add((canonPath, bytes));
                return true;
            }
        }

        private List<(string canonPath, byte[] bytes)> EndCollecting()
        {
            lock (_lock)
            {
                _isCollecting = false;

                if (_files.Count == 0)
                    return new List<(string canonPath, byte[] bytes)>();

                var files = new List<(string canonPath, byte[] bytes)>(_files);
                _files.Clear();
                return files;
            }
        }
    }
}
