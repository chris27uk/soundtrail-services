using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.Enrichment.Scheduler;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure;
using Soundtrail.Services.Internal.Projector;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.ServiceDefaults;

namespace Soundtrail.Services.Tests.Integration.Composition;

internal static class ProductionCompositionTestEnvironment
{
    public static void ValidateApiComposition()
    {
        var builder = CreateBuilder(Environments.Development);
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

    public static async Task ValidateApiCanRouteKnownMusicDataMessageAsync()
    {
        await using var app = BuildApiApplication("Testing");
        await app.StartAsync();

        await using var scope = app.Services.CreateAsyncScope();
        var commandBus = scope.ServiceProvider.GetRequiredService<Soundtrail.Domain.Abstractions.ICommandBus>();
        commandBus.GetType().FullName.Should().Be("Soundtrail.Adapters.Messaging.AzureServiceBusCommandBus");

        await app.StopAsync();
    }

    public static async Task ValidateApiCanRouteUnknownMusicDataMessageAsync()
    {
        await using var app = BuildApiApplication("Testing");
        await app.StartAsync();

        await using var scope = app.Services.CreateAsyncScope();
        var commandBus = scope.ServiceProvider.GetRequiredService<Soundtrail.Domain.Abstractions.ICommandBus>();
        commandBus.GetType().FullName.Should().Be("Soundtrail.Adapters.Messaging.AzureServiceBusCommandBus");

        await app.StopAsync();
    }

    public static void ValidateSchedulerComposition()
    {
        var builder = CreateBuilder(Environments.Development);

        builder.Services.AddFeatures<SchedulerAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        RemoveRavenDatabaseHostedService(builder.Services);

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
        var builder = CreateBuilder(Environments.Development);
        builder.Services.AddAzureServiceBusMessageProcessing(builder.Configuration, builder.Environment);

        builder.Services.AddFeatures<Soundtrail.Services.Enrichment.Worker.WorkerAssemblyMarker>();
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
        var builder = CreateBuilder(Environments.Development);
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

        RemoveRavenDatabaseHostedService(builder.Services);

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

    public static async Task ValidateOrchestratorRegistersKnownMusicDataListenerAsync()
    {
        await using var app = BuildOrchestratorApplication("Testing");
        await app.StartAsync();

        AssertAzureServiceBusListenerRegistered<KnownMusicDataRequestedCommandDto, RequestKnownMusicDataMessage>(app.Services);

        await app.StopAsync();
    }

    public static async Task ValidateOrchestratorRegistersUnknownMusicDataListenerAsync()
    {
        await using var app = BuildOrchestratorApplication("Testing");
        await app.StartAsync();

        AssertAzureServiceBusListenerRegistered<UnknownMusicDataRequestedCommandDto, RequestUnknownMusicDataMessage>(app.Services);

        await app.StopAsync();
    }

    public static async Task ValidateOrchestratorRegistersAssessMusicCatalogItemListenerAsync()
    {
        await using var app = BuildOrchestratorApplication("Testing");
        await app.StartAsync();

        AssertAzureServiceBusListenerRegistered<AssessMusicCatalogItemCommandDto, AssessWorkMessage>(app.Services);

        await app.StopAsync();
    }

    public static async Task ValidateOrchestratorRegistersDispatchLookupWorkListenerAsync()
    {
        await using var app = BuildOrchestratorApplication("Testing");
        await app.StartAsync();

        AssertAzureServiceBusListenerRegistered<DispatchLookupWorkCommandDto, DispatchLookupWork>(app.Services);

        await app.StopAsync();
    }

    public static async Task ValidateOrchestratorRegistersLookupCompletedListenerAsync()
    {
        await using var app = BuildOrchestratorApplication("Testing");
        await app.StartAsync();

        AssertAzureServiceBusListenerRegistered<CatalogLookupCompletedCommandDto, CatalogLookupCompleted>(app.Services);

        await app.StopAsync();
    }

    public static void ValidateProjectorComposition()
    {
        var builder = CreateBuilder(Environments.Development);
        builder.Services.AddAzureServiceBusMessageProcessing(builder.Configuration, builder.Environment);

        builder.Services.AddFeatures<ProjectorAssemblyMarker>();
#pragma warning disable ASP0000
        using var bootstrapProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
        var features = bootstrapProvider.GetServices<IFeature>().ToArray();

        foreach (var feature in features)
        {
            feature.ConfigureServices(builder.Services, builder.Configuration);
        }

        RemoveRavenDatabaseHostedService(builder.Services);

        builder.Services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Select(descriptor => descriptor.ImplementationType?.FullName)
            .Should()
            .Contain([
                "Soundtrail.Services.Internal.Projector.Infrastructure.Messaging.CatalogProjectionSubscriptionService",
                "Soundtrail.Services.Internal.Projector.Infrastructure.Messaging.DiscoveryProjectionSubscriptionService"
            ]);

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

    private static WebApplication BuildApiApplication(string environmentName)
    {
        var builder = CreateBuilder(environmentName);
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

    private static WebApplication BuildOrchestratorApplication(string environmentName)
    {
        var builder = CreateBuilder(environmentName);
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

        RemoveRavenDatabaseHostedService(builder.Services);

        var app = builder.Build();

        foreach (var feature in features.OfType<IOrchestratorFeature>())
        {
            feature.ConfigureApplication(app);
        }

        app.MapDefaultEndpoints();
        return app;
    }

    private static WebApplicationBuilder CreateBuilder(string environmentName)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                EnvironmentName = environmentName
            });
        builder.WebHost.UseTestServer();

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
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

    private static void AssertAzureServiceBusListenerRegistered<TDto, TDomain>(IServiceProvider services)
    {
        services.GetServices<IHostedService>()
            .Should()
            .Contain(service => IsAzureServiceBusListenerFor<TDto, TDomain>(service.GetType()));
    }

    private static bool IsAzureServiceBusListenerFor<TDto, TDomain>(Type serviceType)
    {
        return serviceType.IsGenericType
            && serviceType.Name == "AzureServiceBusMessageListenerHostedService`2"
            && serviceType.GenericTypeArguments.Length == 2
            && serviceType.GenericTypeArguments[0] == typeof(TDto)
            && serviceType.GenericTypeArguments[1] == typeof(TDomain);
    }

    private static void RemoveRavenDatabaseHostedService(IServiceCollection services)
    {
        foreach (var descriptor in services
                     .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                         && descriptor.ImplementationType?.FullName is "Soundtrail.Adapters.Persistence.RavenDatabaseHostedService")
                     .ToArray())
        {
            services.Remove(descriptor);
        }
    }
}
