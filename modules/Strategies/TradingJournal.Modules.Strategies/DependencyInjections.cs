using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TradingJournal.Modules.Strategies.Infrastructure;
using TradingJournal.Modules.Strategies.Services;
using TradingJournal.Shared.Behaviors;

namespace TradingJournal.Modules.Strategies;

public static class DependencyInjections
{
    public static IServiceCollection AddStrategyModule(this IServiceCollection services, IConfiguration configuration,
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

        services.AddScoped<IStrategyDbContext, StrategyDbContext>();
        services.AddScoped<IHistoricalDataService, HistoricalDataService>();

        services.AddDbContext<StrategyDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("TradeDatabase"));
        });

        return services;
    }

    public static async Task<IApplicationBuilder> MigratePsychologyDatabase(this IApplicationBuilder app)
    {
        try
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();

            StrategyDbContext dbContext = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();
            await dbContext.Database.MigrateAsync();

        }
        catch { }
        return app;
    }
}
