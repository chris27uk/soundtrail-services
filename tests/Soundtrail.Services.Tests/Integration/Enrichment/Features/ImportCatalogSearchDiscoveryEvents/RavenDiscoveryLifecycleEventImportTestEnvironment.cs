using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ImportCatalogSearchDiscoveryEvents;
using Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ReplayDiscoveryLifecycleProjection.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ImportCatalogSearchDiscoveryEvents;

internal sealed class RavenDiscoveryLifecycleEventImportTestEnvironment : IAsyncDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;
    private readonly ServiceProvider serviceProvider;
    private readonly ProjectDiscoveryLifecycleSubscriptionHostedService hostedService;

    private RavenDiscoveryLifecycleEventImportTestEnvironment(
        RavenEmbeddedTestDatabase raven,
        ServiceProvider serviceProvider,
        ProjectDiscoveryLifecycleSubscriptionHostedService hostedService)
    {
        this.raven = raven;
        this.serviceProvider = serviceProvider;
        this.hostedService = hostedService;
    }

    public static async Task<RavenDiscoveryLifecycleEventImportTestEnvironment> CreateAsync()
    {
        var raven = RavenEmbeddedTestDatabase.Create();

        var services = new ServiceCollection();
        services.AddSingleton<IDocumentStore>(raven.Store);
        services.AddScoped<IAsyncDocumentSession>(_ => raven.Store.OpenAsyncSession());
        services.AddScoped<ProjectDiscoveryLifecycleHandler>();
        services.AddScoped<ILoadDiscoveryLifecycleProjectionPort, RavenLoadDiscoveryLifecycleProjection>();
        services.AddScoped<ISaveDiscoveryLifecycleProjectionPort, RavenSaveDiscoveryLifecycleProjection>();
        services.AddSingleton<RavenDiscoveryLifecycleProjectionMapper>();

        var serviceProvider = services.BuildServiceProvider();
        var hostedService = new ProjectDiscoveryLifecycleSubscriptionHostedService(
            raven.Store,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ProjectDiscoveryLifecycleSubscriptionHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        return new RavenDiscoveryLifecycleEventImportTestEnvironment(raven, serviceProvider, hostedService);
    }

    public async Task ImportAsync(ImportCatalogSearchDiscoveryEventsCommand command)
    {
        var handler = new ImportCatalogSearchDiscoveryEventsHandler(
            new RavenCatalogSearchDiscoveryRepository(raven.Store));
        await handler.Handle(command, CancellationToken.None);
    }

    public async Task<CatalogSearchStatusRecordDto?> WaitForStatusAsync(
        CatalogSearchCriteria criteria,
        TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            var status = await LoadStatusAsync(criteria);
            if (status is not null)
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return await LoadStatusAsync(criteria);
    }

    public async Task<CatalogSearchStatusRecordDto?> LoadStatusAsync(CatalogSearchCriteria criteria)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogSearchStatusRecordDto>(
            CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
            CancellationToken.None);
    }

    public async Task ReplayDiscoveryProjectionAsync()
    {
        using var querySession = raven.Store.OpenAsyncSession();
        var streamMetadata = await querySession.Advanced.LoadStartingWithAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            "discovery-query-streams/");
        var criteriaValues = streamMetadata.Select(x => x.Criteria).ToList();

        using var session = raven.Store.OpenAsyncSession();
        var replayHandler = new ReplayDiscoveryLifecycleProjectionHandler(
            new RavenLoadStoredDiscoveryLifecycleEvents(session),
            new ProjectDiscoveryLifecycleHandler(
                new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper())));

        foreach (var criteria in criteriaValues.Distinct(StringComparer.Ordinal))
        {
            await replayHandler.Handle(
                new ReplayDiscoveryLifecycleProjectionCommand(CatalogSearchCriteria.From(criteria)),
                CancellationToken.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await hostedService.StopAsync(CancellationToken.None);
        await serviceProvider.DisposeAsync();
        raven.Dispose();
    }
}
