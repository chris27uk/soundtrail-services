using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Messaging.Asb;
using Soundtrail.Adapters.Projection;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.Enrichment.Worker;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Startup;
using Soundtrail.Services.Internal.Projector;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.ServiceDefaults;
using Soundtrail.Services.Tests.EndToEnd.Shared;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd;

public sealed class EndToEndHostFixture : IAsyncLifetime
{
    public const string EnvironmentName = "EndToEnd";

    private static readonly Lazy<Task<SharedHosts>> Shared = new(StartSharedAsync);

    private SharedHosts? hosts;

    public HttpClient ApiClient =>
        this.hosts?.ApiClient ?? throw new InvalidOperationException("End-to-end hosts have not been started.");

    public IDocumentStore DocumentStore =>
        this.hosts?.DocumentStore ?? throw new InvalidOperationException("End-to-end hosts have not been started.");

    /// <summary>
    /// Begins shared host startup so container/host work overlaps the parallel suite.
    /// </summary>
    public static void EnsureWarmupStarted() => _ = Shared.Value;

    public async Task InitializeAsync()
    {
        this.hosts = await Shared.Value;
    }

    public Task DisposeAsync()
    {
        // Shared hosts live for the process so background warmup stays valid.
        return Task.CompletedTask;
    }

    private static async Task<SharedHosts> StartSharedAsync()
    {
        var databaseName = $"soundtrail-e2e-{Guid.NewGuid():N}";
        var serviceBusTask = LocalServiceBusEmulator.StartAsync();
        var redisTask = LocalRedisTestServer.StartAsync();
        var ravenUrlTask = EmbeddedRavenTestServer.GetServerUrlAsync();
        await Task.WhenAll(serviceBusTask, redisTask, ravenUrlTask);

        var serviceBus = await serviceBusTask;
        var redis = await redisTask;
        var ravenUrl = await ravenUrlTask;
        var providerStubs = ProviderStubServer.Start();
        var documentStore = EmbeddedRavenTestServer.CreateDocumentStore(databaseName);
        var configuration = BuildConfiguration(
            serviceBus.ConnectionString,
            ravenUrl,
            databaseName,
            redis.ConnectionString,
            providerStubs.BaseUrl);

        var api = BuildApi(configuration, documentStore);
        var orchestrator = BuildOrchestrator(configuration, documentStore);
        var worker = BuildWorker(configuration, documentStore);
        var projector = BuildProjector(configuration, documentStore);

        await projector.StartAsync();
        await orchestrator.StartAsync();
        await worker.StartAsync();
        await api.StartAsync();

        return new SharedHosts(
            documentStore,
            api.GetTestClient(),
            api,
            orchestrator,
            worker,
            projector,
            providerStubs);
    }

    private static Dictionary<string, string?> BuildConfiguration(
        string serviceBusConnectionString,
        string ravenUrl,
        string databaseName,
        string redisConnectionString,
        string providerBaseUrl) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ServiceBus:ConnectionString"] = serviceBusConnectionString,
            ["RavenDb:Urls:0"] = ravenUrl,
            ["RavenDb:Database"] = databaseName,
            ["ConnectionStrings:Redis"] = redisConnectionString,
            ["LookupExecutionAdmission:ActiveLeaseSeconds"] = "300",
            ["LookupExecutionAdmission:KeyPrefix"] = $"lookup-execution-admission-e2e-{Guid.NewGuid():N}",
            ["Kworb:BaseUrl"] = providerBaseUrl,
            ["MusicBrainz:BaseUrl"] = providerBaseUrl,
            ["MusicBrainz:UserAgent"] = "Soundtrail.Services.Tests/1.0",
            ["Odesli:BaseUrl"] = providerBaseUrl,
            ["Odesli:UserCountry"] = "US",
            ["SourceBudgets:Kworb:MaxRequests"] = "100000",
            ["SourceBudgets:Kworb:WindowSeconds"] = "60",
            ["SourceBudgets:Kworb:SafetyMarginPercent"] = "0",
            ["SourceBudgets:Kworb:MinimumSpacingSeconds"] = "0",
            ["SourceBudgets:MusicBrainz:MaxRequests"] = "100000",
            ["SourceBudgets:MusicBrainz:WindowSeconds"] = "60",
            ["SourceBudgets:MusicBrainz:SafetyMarginPercent"] = "0",
            ["SourceBudgets:MusicBrainz:MinimumSpacingSeconds"] = "0",
            ["SourceBudgets:Odesli:MaxRequests"] = "100000",
            ["SourceBudgets:Odesli:WindowSeconds"] = "60",
            ["SourceBudgets:Odesli:SafetyMarginPercent"] = "0",
            ["SourceBudgets:Odesli:MinimumSpacingSeconds"] = "0",
            ["PlanningAssessment:DefaultDeferredSeconds"] = "0"
        };

    private static WebApplication BuildApi(
        IReadOnlyDictionary<string, string?> configuration,
        IDocumentStore documentStore)
    {
        var builder = CreateBuilder(configuration);
        builder.Services.AddSingleton(documentStore);
        builder.Services.AddCatalogSearchAttemptQueue(builder.Configuration);

        builder.Services.AddFeatures<ApiAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        RemoveRavenDatabaseHostedService(builder.Services);

        var app = builder.Build();
        foreach (var feature in features.OfType<IApiFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
        return app;
    }

    private static WebApplication BuildOrchestrator(
        IReadOnlyDictionary<string, string?> configuration,
        IDocumentStore documentStore)
    {
        var builder = CreateBuilder(configuration);
        builder.Services.AddSingleton(documentStore);
        builder.Services.AddAzureServiceBusMessageProcessing(builder.Configuration, builder.Environment);

        builder.Services.AddFeatures<OrchestratorAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        HandlerCollection.AddMessageHandlersFromAssemblies(builder.Services, typeof(OrchestratorAssemblyMarker));
        RemoveRavenDatabaseHostedService(builder.Services);

        var app = builder.Build();
        foreach (var feature in features.OfType<IOrchestratorFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
        return app;
    }

    private static WebApplication BuildWorker(
        IReadOnlyDictionary<string, string?> configuration,
        IDocumentStore documentStore)
    {
        var builder = CreateBuilder(configuration);
        builder.Services.AddSingleton(documentStore);
        builder.Services.AddAzureServiceBusMessageProcessing(builder.Configuration, builder.Environment);
        builder.Services.AddWorkerStartupValidation(builder.Configuration);

        builder.Services.AddFeatures<WorkerAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        HandlerCollection.AddMessageHandlersFromAssemblies(builder.Services, typeof(WorkerAssemblyMarker));
        RemoveRavenDatabaseHostedService(builder.Services);

        var app = builder.Build();
        app.MapDefaultEndpoints();
        return app;
    }

    private static WebApplication BuildProjector(
        IReadOnlyDictionary<string, string?> configuration,
        IDocumentStore documentStore)
    {
        var builder = CreateBuilder(configuration);
        builder.Services.AddSingleton(documentStore);

        builder.Services.AddFeatures<ProjectorAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        HandlerCollection.AddFromAssemblies(builder.Services, typeof(ProjectorAssemblyMarker));
        RemoveRavenDatabaseHostedService(builder.Services);

        var app = builder.Build();
        foreach (var feature in features.OfType<IProjectorFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
        return app;
    }

    private static WebApplicationBuilder CreateBuilder(IReadOnlyDictionary<string, string?> configuration)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = EnvironmentName
            });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.AddServiceDefaults();
        return builder;
    }

    private static void RemoveRavenDatabaseHostedService(IServiceCollection services)
    {
        foreach (var descriptor in services
                     .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                         && descriptor.ImplementationType?.FullName is
                             "Soundtrail.Adapters.Persistence.RavenDatabaseHostedService"
                             or "Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.RavenDatabaseHostedService")
                     .ToArray())
        {
            services.Remove(descriptor);
        }
    }

    private sealed record SharedHosts(
        IDocumentStore DocumentStore,
        HttpClient ApiClient,
        WebApplication Api,
        WebApplication Orchestrator,
        WebApplication Worker,
        WebApplication Projector,
        ProviderStubServer ProviderStubs);
}

[CollectionDefinition(nameof(EndToEndHostCollection))]
public sealed class EndToEndHostCollection : ICollectionFixture<EndToEndHostFixture>;
