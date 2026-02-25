using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Contracts;
using TradingJournal.Shared.Interfaces;

namespace TradingJournal.Modules.Psychology.Infrastructure;

internal sealed class EmotionTagCacheWarmupEntry : ICacheWarmupEntry
{
    public string CacheKey => CacheKeys.EmotionTags;

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(25);

    public TimeSpan CacheTtl => TimeSpan.FromMinutes(30);

    public async Task WarmAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        IPsychologyDbContext context = scope.ServiceProvider.GetRequiredService<IPsychologyDbContext>();
        ICacheRepository cacheRepository = scope.ServiceProvider.GetRequiredService<ICacheRepository>();

        List<EmotionTagCacheDto> cacheDtos = await context.EmotionTags
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(e => new EmotionTagCacheDto { Id = e.Id, Name = e.Name })
            .ToListAsync(cancellationToken);

        await cacheRepository.UpdateCache(
            CacheKey,
            _ => Task.FromResult(cacheDtos),
            CacheTtl,
            cancellationToken);
    }
}
