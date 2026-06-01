using System;
using System.Collections.Concurrent;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class ExpiringCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, Entry> _entries = new();

        internal bool TryGetFresh(TKey key, out TValue value)
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

        internal void SetFor(TKey key, TValue value, TimeSpan ttl)
            => _entries[key] = new Entry(value, DateTime.UtcNow.Add(ttl));

        internal void Invalidate(TKey key)
            => _entries.TryRemove(key, out _);

        private readonly struct Entry
        {
            internal Entry(TValue value, DateTime expiry)
            {
                Value = value;
                Expiry = expiry;
            }

            internal TValue Value { get; }
            internal DateTime Expiry { get; }
        }
    }
}
