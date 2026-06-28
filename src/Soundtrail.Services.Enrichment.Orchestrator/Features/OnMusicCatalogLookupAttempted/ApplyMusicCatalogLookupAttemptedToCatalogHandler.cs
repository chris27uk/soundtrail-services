using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

public sealed class ApplyMusicCatalogLookupAttemptedToCatalogHandler(
    IMusicTrackEventRepository eventRepository) : IHandler<ApplyMusicCatalogLookupAttemptedToCatalogCommand>
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

        var aggregate = await CatalogEntityAggregate.LoadAsync(
            eventRepository,
            fetched.MusicCatalogId,
            cancellationToken);
        aggregate.RecordMusicCatalogMetadataFetched(fetched);
        await aggregate.SaveAsync(eventRepository, fetched.CommandId, cancellationToken);
    }
}
