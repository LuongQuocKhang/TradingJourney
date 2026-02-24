using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TradingJournal.Shared.Behaviors;
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


    public static IServiceCollection AddMediatRBehaviors(this IServiceCollection services, bool isDevelopment = false)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

            config.AddOpenBehavior(typeof(ValidationBehavior<,>));

            if (isDevelopment)
            {
                config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            }
        });

        return services;
    }
}
