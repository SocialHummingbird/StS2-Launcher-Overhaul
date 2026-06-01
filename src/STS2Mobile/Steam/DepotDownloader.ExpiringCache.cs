using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class ExpiringCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly struct CacheEntry
        {
            internal CacheEntry(TValue value, DateTime expiry)
            {
                Value = value;
                Expiry = expiry;
            }

            internal TValue Value { get; }
            internal DateTime Expiry { get; }
            internal bool Fresh => DateTime.UtcNow < Expiry;

            internal static CacheEntry Create(TValue value, DateTime expiry)
                => new(value, expiry);
        }

        private readonly ConcurrentDictionary<TKey, CacheEntry> _entries = new();

        private bool TryGetFresh(TKey key, out TValue value)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                if (entry.Fresh)
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
            => _entries[key] = CacheEntry.Create(value, DateTime.UtcNow.Add(ttl));

        internal async Task<TValue> GetOrAddAsync(
            TKey key,
            TimeSpan ttl,
            Func<Task<TValue>> fetch,
            Func<TValue, bool>? shouldCache = null
        )
        {
            if (TryGetFresh(key, out var cached))
                return cached;

            var value = await fetch();
            if (shouldCache?.Invoke(value) != false)
                SetFor(key, value, ttl);
            return value;
        }

        internal void Invalidate(TKey key)
            => _entries.TryRemove(key, out _);
    }
}
