using MassTransit;
using TradingJournal.Jobs.BackTestEngine.Consumers;
using TradingJournal.Modules.Strategies;
using TradingJournal.Shared;

var builder = Host.CreateApplicationBuilder(args);

// Ensure appsettings.json or environment variables are loaded
var configuration = builder.Configuration;

bool isDevelopment = builder.Environment.IsDevelopment();

// Register Modules and Shared Services
builder.Services
    .AddSharedModule()
    .AddStrategyModule(configuration, isDevelopment);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<RunBacktestEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Register endpoints for consumers
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
await host.RunAsync();
