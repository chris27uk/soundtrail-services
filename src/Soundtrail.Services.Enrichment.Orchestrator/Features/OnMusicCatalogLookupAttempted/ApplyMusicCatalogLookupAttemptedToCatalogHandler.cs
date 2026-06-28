using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

public sealed class ApplyMusicCatalogLookupAttemptedToCatalogHandler(
    IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> eventRepository) : IHandler<ApplyMusicCatalogLookupAttemptedToCatalogCommand>
{
    public async Task Handle(
        ApplyMusicCatalogLookupAttemptedToCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var fetched = command.Attempted.MusicCatalogMetadataFetched;
        if (fetched is null)
        {
            return;
        }

        var loaded = await MusicTrack.LoadAsync(
            eventRepository,
            fetched.MusicCatalogId,
            cancellationToken);
        loaded.Aggregate.MetadataFetched(fetched);
        await loaded.Aggregate.SaveAsync(eventRepository, loaded.Stream, fetched.CommandId, cancellationToken);
    }
}
