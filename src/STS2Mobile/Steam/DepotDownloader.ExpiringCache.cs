using System;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class ExpiringCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, Entry> _entries = new();

        private bool TryGetFresh(TKey key, out TValue value)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow < entry.Expiry)
                {
                    value = entry.Value;
                    return true;
                }

                _entries.TryRemove(key, out _);
            }

            value = default!;
            return false;
        }

        private void SetFor(TKey key, TValue value, TimeSpan ttl)
            => _entries[key] = new Entry(value, DateTime.UtcNow.Add(ttl));

        private void Invalidate(TKey key)
            => _entries.TryRemove(key, out _);

        private readonly struct Entry
        {
            private Entry(TValue value, DateTime expiry)
            {
                Value = value;
                Expiry = expiry;
            }

            private TValue Value { get; }
            private DateTime Expiry { get; }
        }
    }
}
