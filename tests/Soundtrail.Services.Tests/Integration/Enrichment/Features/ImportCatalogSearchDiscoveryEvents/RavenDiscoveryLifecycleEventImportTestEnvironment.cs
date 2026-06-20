using Raven.Client.Documents;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportCatalogSearchDiscoveryEvents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ReplayDiscoveryLifecycleProjection.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Linq;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ImportCatalogSearchDiscoveryEvents;

internal sealed class RavenDiscoveryLifecycleEventImportTestEnvironment : IAsyncDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;

    private RavenDiscoveryLifecycleEventImportTestEnvironment(RavenEmbeddedTestDatabase raven)
    {
        this.raven = raven;
    }

    public static RavenDiscoveryLifecycleEventImportTestEnvironment Create() => new(RavenEmbeddedTestDatabase.Create());

    public async Task ImportAsync(ImportCatalogSearchDiscoveryEventsCommand command)
    {
        var handler = new ImportCatalogSearchDiscoveryEventsHandler(new RavenCatalogSearchDiscoveryRepository(raven.Store));
        await handler.Handle(command, CancellationToken.None);
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
        var storedEvents = await querySession.Advanced.AsyncDocumentQuery<DiscoveryQueryStoredEventRecordDto>()
            .ToListAsync(CancellationToken.None);
        var criteriaValues = storedEvents.Select(x => x.Criteria).ToList();

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

    public ValueTask DisposeAsync()
    {
        raven.Dispose();
        return ValueTask.CompletedTask;
    }
}
