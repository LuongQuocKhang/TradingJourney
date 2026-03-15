using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TradingJournal.Shared.Behaviors;

namespace TradingJournal.Modules.Analytics;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services,
        bool isDevelopment = false)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

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
