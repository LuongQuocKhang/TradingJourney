using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingJournal.Modules.Trades.Infrastructure;
using TradingJournal.Shared;

namespace TradingJournal.Modules.Trades;

public static class DependencyInjection
{
    public static IServiceCollection AddTradeModule(this IServiceCollection services, IConfiguration configuration,
        bool isDevelopment = false)
    {
        services.AddFluentValidators();

        services.AddMediatRBehaviors(isDevelopment);

        services.AddDbContext<TradeDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("TradingJournalDbContext"));
        });

        services.AddScoped<ITradeDbContext, TradeDbContext>();

        return services;
    }
}