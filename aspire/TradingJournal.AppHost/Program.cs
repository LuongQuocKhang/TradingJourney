using Microsoft.Extensions.Configuration;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

#region PostgreSQL
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

IResourceBuilder<PostgresDatabaseResource> tradingHistoryDb = postgres.AddDatabase("tradingHistoryDb");
IResourceBuilder<PostgresDatabaseResource> psychologyDb = postgres.AddDatabase("psychologyDb");
#endregion

#region Redis
IResourceBuilder<ParameterResource> redisPassword = builder.AddParameter("redisPassword",
    secret: true,
    value: builder.Configuration.GetValue<string>("Aspire:StackExchange:Redis:redisPassword") ?? string.Empty);

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight()
    .WithPassword(redisPassword);

#endregion

#region Modules
IResourceBuilder<ProjectResource> tradingJournalModulesTrades = builder.AddProject<Projects.TradingJournal_Modules_Trades>("tradingjournal-modules-trades")
    .WithReference(tradingHistoryDb)
    .WaitFor(tradingHistoryDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithDeveloperCertificateTrust(true);

builder.AddProject<Projects.TradingJournal_Jobs_BackTestEngine>("tradingjournal-jobs-backtestengine");

IResourceBuilder<ProjectResource> tradingJournalModulesAnalytics = builder.AddProject<Projects.TradingJournal_Modules_Analytics>("tradingjournal-modules-analytics");

IResourceBuilder<ProjectResource> tradingJournalModulesPlaybook = builder.AddProject<Projects.TradingJournal_Modules_Playbook>("tradingjournal-modules-playbook");

IResourceBuilder<ProjectResource> tradingJournalModulesPsychology = builder.AddProject<Projects.TradingJournal_Modules_Psychology>("tradingjournal-modules-psychology")
    .WithReference(psychologyDb)
    .WaitFor(psychologyDb)
    .WithReference(redis)
    .WaitFor(redis);

IResourceBuilder<ProjectResource> tradingJournalModulesStrategies = builder.AddProject<Projects.TradingJournal_Modules_Strategies>("tradingjournal-modules-strategies");

#endregion

builder.AddProject<Projects.TradingJournal_ApiGateWay>("tradingjournal-apigateway")
    .WithReference(tradingJournalModulesTrades)
    .WithDeveloperCertificateTrust(true)
    .WaitFor(tradingJournalModulesTrades)
    .WithReference(tradingJournalModulesAnalytics)
    .WaitFor(tradingJournalModulesAnalytics)
    .WithReference(tradingJournalModulesPlaybook)
    .WaitFor(tradingJournalModulesPlaybook)
    .WithReference(tradingJournalModulesPsychology)
    .WaitFor(tradingJournalModulesPsychology)
    .WithReference(tradingJournalModulesStrategies)
    .WaitFor(tradingJournalModulesStrategies)
    ;



await builder.Build()
    .RunAsync();
