using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Shared.Repositories;

/// <summary>
/// A single BackgroundService that runs all registered <see cref="ICacheWarmupEntry"/> instances.
/// Each entry warms its own cache key on startup and refreshes periodically.
/// </summary>
public sealed class SharedCacheWarmupService(
    IServiceScopeFactory scopeFactory,
    IEnumerable<ICacheWarmupEntry> entries,
    ILogger<SharedCacheWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run all warmup entries in parallel, each with its own refresh loop
        IEnumerable<Task> tasks = entries.Select(entry => RunEntryLoopAsync(entry, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunEntryLoopAsync(ICacheWarmupEntry entry, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                await entry.WarmAsync(scope, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to warm cache: {CacheKey}", entry.CacheKey);
            }

            await Task.Delay(entry.RefreshInterval, stoppingToken);
        }
    }
}
