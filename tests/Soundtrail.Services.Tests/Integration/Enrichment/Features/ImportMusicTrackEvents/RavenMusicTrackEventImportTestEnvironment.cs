using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Support;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ImportMusicTrackEvents;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ImportMusicTrackEvents;

internal sealed class RavenMusicTrackEventImportTestEnvironment : IAsyncDisposable
{
    private readonly RavenEmbeddedTestDatabase raven;

    private RavenMusicTrackEventImportTestEnvironment(RavenEmbeddedTestDatabase raven)
    {
        this.raven = raven;
    }

    public static RavenMusicTrackEventImportTestEnvironment Create() => new(RavenEmbeddedTestDatabase.Create());

    public async Task ImportAsync(ImportMusicTrackEventsCommand command)
    {
        using var session = raven.Store.OpenAsyncSession();
        var handler = new ImportMusicTrackEventsHandler(new RavenMusicTrackStreamStore(session));
        await handler.Handle(command, CancellationToken.None);
        await session.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<CatalogTrackRecordDto?> LoadCatalogTrackAsync(string trackId)
    {
        using var session = raven.Store.OpenAsyncSession();
        return await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(trackId),
            CancellationToken.None);
    }

    public async Task ReplayCatalogProjectionAsync()
    {
        using var session = raven.Store.OpenAsyncSession();
        var applier = new CatalogMusicTrackProjectionApplier(new CatalogProjectionMutationService());
        var events = await session.Advanced.AsyncDocumentQuery<MusicTrackStoredEventRecordDto>()
            .ToListAsync(CancellationToken.None);

        foreach (var storedEvent in events.OrderBy(x => x.MusicCatalogId, StringComparer.Ordinal).ThenBy(x => x.Version))
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
