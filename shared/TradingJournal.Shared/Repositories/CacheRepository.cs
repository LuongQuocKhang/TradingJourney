using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Shared.Repositories;

public class CacheRepository(IDistributedCache distributedCache) : ICacheRepository
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(30);

    public async Task<T?> GetOrCreateAsync<T>(string key,
        Func<CancellationToken, Task<T>> handle,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default)
    {
        string? cached = await distributedCache.GetStringAsync(key, cancellationToken);

        if (cached is not null)
        {
            return JsonSerializer.Deserialize<T>(cached);
        }

        T result = await handle(cancellationToken);

        TimeSpan expiredTime = expiration ?? DefaultExpiration;

        DistributedCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = expiredTime
        };

        string serialized = JsonSerializer.Serialize(result);
        await distributedCache.SetStringAsync(key, serialized, entryOptions, cancellationToken);

        return result;
    }

    public async Task UpdateCache<T>(string key, Func<CancellationToken, Task<T>> handle, TimeSpan? expiration, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken);

        T? value = await handle(cancellationToken);

        TimeSpan expiredTime = expiration ?? DefaultExpiration;

        DistributedCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = expiredTime
        };

        string serialized = JsonSerializer.Serialize(value);
        await distributedCache.SetStringAsync(key, serialized, entryOptions, cancellationToken);
    }

    public async Task RemoveCache(string key, CancellationToken cancellationToken = default)
    {
        await distributedCache.RemoveAsync(key, cancellationToken);
    }
}