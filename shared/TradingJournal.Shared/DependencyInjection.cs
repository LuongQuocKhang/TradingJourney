using Microsoft.Extensions.DependencyInjection;
using TradingJournal.Shared.Common;
using TradingJournal.Shared.Interfaces;
using TradingJournal.Shared.Repositories;

namespace TradingJournal.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        services.AddHybridCache();

        services.AddSingleton<ICacheRepository, CacheRepository>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        return services;
    }
}
