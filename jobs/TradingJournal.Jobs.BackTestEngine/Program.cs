using TradingJournal.Jobs.BackTestEngine;
using TradingJournal.ServiceDefaults;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
