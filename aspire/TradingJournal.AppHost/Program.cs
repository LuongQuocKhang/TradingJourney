using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresUserName = builder.AddParameter("postgresUserName", 
    secret: true, 
    value: builder.Configuration.GetValue<string>("Aspire:Npgsql:postgresUserName") ?? string.Empty);

IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("postgresPassword", 
    secret: true,
    value: builder.Configuration.GetValue<string>("Aspire:Npgsql:postgresPassword") ?? string.Empty);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithUserName(postgresUserName)
    .WithPassword(postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<PostgresDatabaseResource> postgresDb = postgres.AddDatabase("postgresdb");

builder.AddProject<Projects.TradingJournal_ApiGateWay>("tradingjournal-apigateway")
    .WithReference(postgresDb)
    .WaitFor(postgresDb);

builder.AddProject<Projects.TradingJournal_Jobs_BackTestEngine>("tradingjournal-jobs-backtestengine");

await builder.Build()
    .RunAsync();
