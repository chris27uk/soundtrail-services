using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.Enrichment.Scheduler;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure;
using Soundtrail.Services.Projector;
using Soundtrail.Services.Projector.Infrastructure;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

namespace Soundtrail.Services.Tests.Integration.Composition;

internal static class ProductionCompositionTestEnvironment
{
    public static void ValidateApiComposition()
    {
        var builder = CreateBuilder();
        builder.Services.AddCatalogSearchAttemptQueue(builder.Configuration);
        builder.Host.UseWolverine(
            options => options.UseApiServiceBusMessaging(builder.Configuration, builder.Environment));

        using var _ = FeatureEnvironment.Live();
        builder.Services.AddFeatures<ApiAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        builder.Services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var app = builder.Build();

        foreach (var feature in features.OfType<IApiFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
    }

    public static void ValidateSchedulerComposition()
    {
        var builder = CreateBuilder();

        using var _ = FeatureEnvironment.Live();
        builder.Services.AddFeatures<SchedulerAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        foreach (var descriptor in builder.Services
                     .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                         && descriptor.ImplementationType?.FullName is "Soundtrail.Adapters.Persistence.RavenDatabaseHostedService")
                     .ToArray())
        {
            builder.Services.Remove(descriptor);
        }

        builder.Host.UseWolverine(
            options =>
            {
                foreach (var feature in features.OfType<ISchedulerFeature>())
                {
                    feature.ConfigureMessaging(options, builder.Configuration, builder.Environment);
                }
            });

        builder.Services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var app = builder.Build();

        foreach (var feature in features.OfType<ISchedulerFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
        app.StartAsync().GetAwaiter().GetResult();
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public static void ValidateWorkerComposition()
    {
        var builder = CreateBuilder();

        using var _ = FeatureEnvironment.Live();
        builder.Services.AddFeatures<Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging.ServiceBusOptions>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        builder.Services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
    }

    public static void ValidateOrchestratorComposition()
    {
        var builder = CreateBuilder();

        using var _ = FeatureEnvironment.Live();
        builder.Services.AddFeatures<OrchestratorAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        foreach (var descriptor in builder.Services
                     .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                         && descriptor.ImplementationType?.FullName is "Soundtrail.Adapters.Persistence.RavenDatabaseHostedService")
                     .ToArray())
        {
            builder.Services.Remove(descriptor);
        }

        builder.Host.UseWolverine(
            options =>
            {
                foreach (var feature in features.OfType<IOrchestratorFeature>())
                {
                    feature.ConfigureMessaging(options, builder.Configuration, builder.Environment);
                }
            });

        builder.Services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var app = builder.Build();

        foreach (var feature in features.OfType<IOrchestratorFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
    }

    public static void ValidateProjectorComposition()
    {
        var builder = CreateBuilder();

        using var _ = FeatureEnvironment.Live();
        builder.Services.AddFeatures<ProjectorAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        foreach (var descriptor in builder.Services
                     .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                         && descriptor.ImplementationType?.FullName is "Soundtrail.Adapters.Persistence.RavenDatabaseHostedService")
                     .ToArray())
        {
            builder.Services.Remove(descriptor);
        }

        builder.Host.UseWolverine(
            options =>
            {
                foreach (var feature in features.OfType<IProjectorFeature>())
                {
                    feature.ConfigureMessaging(options, builder.Configuration, builder.Environment);
                }
            });

        builder.Services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var app = builder.Build();

        foreach (var feature in features.OfType<IProjectorFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
    }

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = Environments.Development
            });
        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "",
                ["ServiceBus:DiscoveryBacklogSchedulingQueueName"] = "discovery-backlog-scheduling",
                ["ServiceBus:PlaylistUpdatesQueueName"] = "playlist-updates",
                ["ServiceBus:CatalogSearchAttemptsQueueName"] = "lookup-music-requests",
                ["ServiceBus:KnownCatalogItemRequestsQueueName"] = "known-catalog-item-requests",
                ["ServiceBus:MusicBrainzLookupQueueName"] = "lookup-musicbrainz",
                ["ServiceBus:PlaybackReferencesLookupQueueName"] = "lookup-playback-references",
                ["ServiceBus:EnrichmentResponsesQueueName"] = "enrichment-responses",
                ["RavenDb:Urls:0"] = "http://localhost:8080",
                ["RavenDb:Database"] = "soundtrail",
                ["TickerQ:ConnectionString"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"soundtrail-scheduler-{Guid.NewGuid():N}.db")}",
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
                ["LookupExecutionAdmission:ActiveLeaseSeconds"] = "300",
                ["LookupExecutionAdmission:KeyPrefix"] = "lookup-execution-admission"
            });

        builder.AddServiceDefaults();
        return builder;
    }
}
