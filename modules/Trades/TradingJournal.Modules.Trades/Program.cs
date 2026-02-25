using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using TradingJournal.Modules.Trades;
using TradingJournal.Modules.Trades.Extensions;
using TradingJournal.ServiceDefaults;
using TradingJournal.Shared;
using TradingJournal.Shared.Middlewares;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache(connectionName: "redis");

// Add services to the container.

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
});

bool isDevelopment = builder.Environment.IsDevelopment();

builder.ConfigureAspireDatabase();

builder.Services
    .AddSharedModule()
    .AddTradeModule(configuration, isDevelopment);

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

app.MapDefaultEndpoints();

app.MapCarter();
app.UseAntiforgery();
app.UseSwaggerDoc();
app.UseStaticFiles();

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
