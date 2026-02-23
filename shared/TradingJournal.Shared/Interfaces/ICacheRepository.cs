namespace TradingJournal.Shared.Interfaces;

public interface ICacheRepository
{
    Task<T?> GetOrCreateAsync<T>(string key,
        Func<CancellationToken, Task<T>> handle,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default);

    Task UpdateCache<T>(string key,
        Func<CancellationToken, Task<T>> handle,
        TimeSpan? expiration,
        CancellationToken cancellationToken = default);
    
    Task RemoveCache(string key, CancellationToken cancellationToken = default);
}