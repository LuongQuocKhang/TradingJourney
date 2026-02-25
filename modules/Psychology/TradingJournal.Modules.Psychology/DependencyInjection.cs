using System.Reflection;
using System.Text.Json.Serialization;
using TradingJournal.Modules.Psychology.Infrastructure.Persistance;
using TradingJournal.Shared.Behaviors;

namespace TradingJournal.Modules.Psychology;

public static class DependencyInjection
{
    public static IServiceCollection AddPsychologyModule(this IServiceCollection services, IConfiguration configuration,
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

        services.AddScoped<IPsychologyDbContext, PsychologyDbContext>();
        
        return services;
    }

    public static WebApplicationBuilder ConfigureAspireDatabase(this WebApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<PsychologyDbContext>("psychologyDb");

        return builder;
    }

    public static async Task<IApplicationBuilder> MigratePsychologyDatabase(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        PsychologyDbContext dbContext = scope.ServiceProvider.GetRequiredService<PsychologyDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}