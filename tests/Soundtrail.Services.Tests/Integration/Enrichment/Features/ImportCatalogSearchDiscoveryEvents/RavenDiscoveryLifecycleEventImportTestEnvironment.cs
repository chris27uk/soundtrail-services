using Raven.Client.Documents;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportCatalogSearchDiscoveryEvents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

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
        var events = await querySession.Advanced.AsyncDocumentQuery<DiscoveryQueryStoredEventRecordDto>()
            .ToListAsync(CancellationToken.None);

        using var session = raven.Store.OpenAsyncSession();
        var handler = new ProjectDiscoveryLifecycleHandler(
            new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
            new RavenSaveDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()));

        foreach (var stream in events.OrderBy(x => x.Criteria, StringComparer.Ordinal).ThenBy(x => x.Version).GroupBy(x => x.Criteria, StringComparer.Ordinal))
        {
            await handler.Handle(
                new ProjectDiscoveryLifecycleCommand(
                    CatalogSearchCriteria.From(stream.Key),
                    stream.Select(item => item.ToDomainEvent()).ToArray()),
                CancellationToken.None);
        }
    }

    public ValueTask DisposeAsync()
    {
        raven.Dispose();
        return ValueTask.CompletedTask;
    }
}
