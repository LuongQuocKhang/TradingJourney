using Google.GenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TradingJournal.Modules.Trades.EventHandlers;
using TradingJournal.Modules.Trades.Events;
using TradingJournal.Modules.Trades.Extensions;
using TradingJournal.Modules.Trades.Options;
using TradingJournal.Modules.Trades.Services;
using TradingJournal.Shared.Behaviors;
using TradingJournal.Shared.MediatR;

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
            config.AddOpenBehavior(typeof(UserAwareBehavior<,>));

            if (isDevelopment)
            {
                config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            }
        });

        services.AddScoped<ITradeDbContext, TradeDbContext>();

        services.AddDbContext<TradeDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("TradeDatabase"));
        });

        services.AddScoped<ITradeProvider, TradeProvider>();

        services.AddGoogleGenAI(configuration);

        services.AddEventHandlers();

        services.AddHelpers();

        return services;
    }

    public static async Task<IApplicationBuilder> MigrateTradingDatabase(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        try
        {
            TradeDbContext dbContext = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception)
        {
            // Log exception if needed
        }

        return app;
    }

    private static IServiceCollection AddGoogleGenAI(this IServiceCollection services, IConfiguration configuration)
    {
        string apiKey = configuration["GoogleGenAI:ApiKey"] ?? throw new InvalidOperationException("Google Gen AI API key is not configured.");

        Client googleGenAiClient = new(
            apiKey: apiKey
        );

        services.AddSingleton(googleGenAiClient);

        services.AddTransient<IGoogleGenAIService, GoogleGenAIService>();
        services.AddTransient<IPromptService, PromptService>();

        services.Configure<GoogleGenAIOptions>(configuration.GetSection("GoogleGenAI"));

        return services;
    }

    private static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        services.AddTransient<INotificationHandler<SummarizeTradingOrderEvent>,
            SummarizeTradingOrderEventHandler>();

        return services;
    }

    private static IServiceCollection AddHelpers(this IServiceCollection services)
    {
        services.AddHttpClient<IImageHelper, ImageHelper>();
        return services;
    }
}
