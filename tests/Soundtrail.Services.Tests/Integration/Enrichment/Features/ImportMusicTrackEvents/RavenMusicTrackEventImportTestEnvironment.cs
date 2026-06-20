using Raven.Client.Documents;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;
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
        using var querySession = raven.Store.OpenAsyncSession();
        var events = await querySession.Advanced.AsyncDocumentQuery<MusicTrackStoredEventRecordDto>()
            .ToListAsync(CancellationToken.None);

        using var session = raven.Store.OpenAsyncSession();
        var handler = new ProjectMusicTrackCatalogHandler(
            new RavenLoadMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(session, new RavenMusicTrackCatalogProjectionMapper()));

        foreach (var stream in events.OrderBy(x => x.MusicCatalogId, StringComparer.Ordinal).ThenBy(x => x.Version).GroupBy(x => x.MusicCatalogId, StringComparer.Ordinal))
        {
            await handler.Handle(
                new ProjectMusicTrackCatalogCommand(
                    MusicCatalogId.From(stream.Key),
                    stream.Select(item => new VersionedMusicTrackEvent(item.Version, item.ToDomainEvent())).ToArray()),
                CancellationToken.None);
        }
    }

    public ValueTask DisposeAsync()
    {
        raven.Dispose();
        return ValueTask.CompletedTask;
    }
}
