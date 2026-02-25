using Microsoft.Extensions.DependencyInjection;

namespace TradingJournal.Shared.Interfaces;

/// <summary>
/// Defines a cache warmup entry for cross-module shared data.
/// Implement this interface for each data type that needs to be pre-warmed in Redis.
/// </summary>
public interface ICacheWarmupEntry
{
    string CacheKey { get; }

    TimeSpan RefreshInterval { get; }

    TimeSpan CacheTtl { get; }

    Task WarmAsync(IServiceScope scope, CancellationToken cancellationToken);
}
