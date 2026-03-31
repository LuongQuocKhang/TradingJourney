using Carter;
using System.Text.Json.Serialization;
using TradingJournal.ApiGateWay.Extensions;
using TradingJournal.Shared;
using TradingJournal.Modules.Analytics;
using TradingJournal.Modules.Trades;
using Scalar.AspNetCore;
using TradingJournal.Shared.Middlewares;
using TradingJournal.Modules.Psychology;
using TradingJournal.Messaging.Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwagger();

ConfigurationManager configuration = builder.Configuration;

builder.Services.AddCors();

builder.Services.AddCarter();

builder.Services.AddAntiforgery();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

bool isDevelopment = builder.Environment.IsDevelopment();


builder.Services
    .AddSharedModule()
    .AddTradeModule(configuration, isDevelopment)
    .AddPsychologyModule(configuration, isDevelopment)
    .AddAnalyticsModule(isDevelopment)
    .AddInMemoryMessageQueue();

builder.Services.AddOpenApi(options =>
{
    options.UseJwtBearerAuthentication();
});

builder.Services.AddHttpContextAccessor();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.MigrateTradingDatabase();
}

app.UseStaticFiles();

app.MapCarter();

app.UseAntiforgery();

app.UseSwaggerDoc();

// app.UseAuthentication();
// app.UseAuthorization();

app.MapOpenApi();

app.UseCustomExceptionHandler();

app.MapScalarApiReference(options =>
{
});

app.UseCors(cors => cors
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseHttpsRedirection();

await app.RunAsync();
