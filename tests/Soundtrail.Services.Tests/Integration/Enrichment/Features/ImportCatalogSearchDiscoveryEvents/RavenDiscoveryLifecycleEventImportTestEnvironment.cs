using Raven.Client.Documents;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportCatalogSearchDiscoveryEvents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
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
        using var session = raven.Store.OpenAsyncSession();
        var applier = new DiscoveryLifecycleProjectionApplier();
        var events = await session.Advanced.AsyncDocumentQuery<DiscoveryQueryStoredEventRecordDto>()
            .ToListAsync(CancellationToken.None);

        foreach (var storedEvent in events.OrderBy(x => x.Criteria, StringComparer.Ordinal).ThenBy(x => x.Version))
        {
            await applier.ApplyStoredEventAsync(storedEvent, session, CancellationToken.None);
        }

        await session.SaveChangesAsync(CancellationToken.None);
    }

    public ValueTask DisposeAsync()
    {
        raven.Dispose();
        return ValueTask.CompletedTask;
    }
}
