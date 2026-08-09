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

    private readonly string databaseName = $"soundtrail-e2e-{Guid.NewGuid():N}";
    private LocalServiceBusEmulator? serviceBus;
    private LocalRedisTestServer? redis;
    private ProviderStubServer? providerStubs;
    private IDocumentStore? documentStore;
    private WebApplication? api;
    private WebApplication? orchestrator;
    private WebApplication? worker;
    private WebApplication? projector;
    private HttpClient? apiClient;

    public HttpClient ApiClient =>
        this.apiClient ?? throw new InvalidOperationException("End-to-end hosts have not been started.");

    public IDocumentStore DocumentStore =>
        this.documentStore ?? throw new InvalidOperationException("End-to-end hosts have not been started.");

    public async Task InitializeAsync()
    {
        this.serviceBus = await LocalServiceBusEmulator.StartAsync();
        this.redis = await LocalRedisTestServer.StartAsync();
        this.providerStubs = ProviderStubServer.Start();
        this.documentStore = EmbeddedRavenTestServer.CreateDocumentStore(this.databaseName);

        var ravenUrl = await EmbeddedRavenTestServer.GetServerUrlAsync();
        var configuration = BuildConfiguration(
            this.serviceBus.ConnectionString,
            ravenUrl,
            this.databaseName,
            this.redis.ConnectionString,
            this.providerStubs.BaseUrl);

        this.api = BuildApi(configuration, this.documentStore);
        this.orchestrator = BuildOrchestrator(configuration, this.documentStore);
        this.worker = BuildWorker(configuration, this.documentStore);
        this.projector = BuildProjector(configuration, this.documentStore);

        await this.projector.StartAsync();
        await this.orchestrator.StartAsync();
        await this.worker.StartAsync();
        await this.api.StartAsync();

        this.apiClient = this.api.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        if (this.apiClient is not null)
        {
            this.apiClient.Dispose();
        }

        await StopAndDisposeAsync(this.api);
        await StopAndDisposeAsync(this.worker);
        await StopAndDisposeAsync(this.orchestrator);
        await StopAndDisposeAsync(this.projector);

        if (this.providerStubs is not null)
        {
            await this.providerStubs.DisposeAsync();
        }

        if (this.redis is not null)
        {
            await this.redis.DisposeAsync();
        }

        if (this.serviceBus is not null)
        {
            await this.serviceBus.DisposeAsync();
        }

        await EmbeddedRavenTestServer.DisposeAsync(this.documentStore);
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
            ["LookupExecutionAdmission:KeyPrefix"] = "lookup-execution-admission-e2e",
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

    private static async Task StopAndDisposeAsync(WebApplication? app)
    {
        if (app is null)
        {
            return;
        }

        try
        {
            await app.StopAsync();
        }
        catch
        {
        }

        await app.DisposeAsync();
    }
}

[CollectionDefinition(nameof(EndToEndHostCollection))]
public sealed class EndToEndHostCollection : ICollectionFixture<EndToEndHostFixture>;
