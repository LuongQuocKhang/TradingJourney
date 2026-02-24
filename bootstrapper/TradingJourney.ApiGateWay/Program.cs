using Carter;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using TradingJournal.ApiGateWay.Extensions;
using TradingJournal.Modules.Trades;
using TradingJournal.Shared;
using TradingJournal.Shared.Middlewares;

var builder = WebApplication.CreateBuilder(args);

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
});

bool isDevelopment = builder.Environment.IsDevelopment();

builder.Services
    .AddSharedModule()
    .AddTradeModule(configuration, isDevelopment);

builder.Services.AddOpenApi(options =>
{
    options.UseJwtBearerAuthentication();
});

builder.Services.AddHttpContextAccessor();

WebApplication app = builder.Build();

app.MapCarter();
app.UseAntiforgery();
app.UseSwaggerDoc();
app.UseStaticFiles();

app.UseAuthentication();
//app.UseAuthorization();

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
