using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using TradingJournal.Shared.Behaviors;

namespace TradingJournal.Modules.Trades;

public static class DependencyInjection
{
    public static IServiceCollection AddTradeModule(this IServiceCollection services, IConfiguration configuration,
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

        services.AddScoped<ITradeDbContext, TradeDbContext>();

        return services;
    }

    public static WebApplicationBuilder ConfigureAspireDatabase(this WebApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<TradeDbContext>("postgresdb");

        return builder;
    }

    public static async Task<IApplicationBuilder> MigrateTradingDatabase(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        TradeDbContext dbContext = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        return app;
    }
}