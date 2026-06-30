using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged;

public sealed class ApplyMusicCatalogLookupHistoryChangedToCatalogHandler(
    IEventStreamRepository<ArtistId, IDomainEvent> catalogRepository) : IHandler<MusicCatalogLookupHistoryChangedCommand>
{
    public async Task Handle(
        MusicCatalogLookupHistoryChangedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var (version, @event) in command.Events)
        {
            if (@event is not MusicCatalogLookupCompleted completed)
            {
                continue;
            }

            var fetched = new MusicCatalogMetadataFetched(
                CommandId.For($"MusicCatalogLookupCompleted:{command.LookupId.StableValue}:{version}"),
                completed.MusicCatalogId,
                completed.SourceProvider,
                completed.Priority,
                completed.CompletedAt,
                completed.Metadata,
                completed.References,
                completed.FailedProviders,
                completed.Hierarchy,
                CorrelationId.From($"lookup:{command.LookupId.StableValue}:{version}"));

            var artistId = ArtistCatalogIdentity.ResolveArtistId(fetched);
            var loaded = await ArtistCatalog.LoadAsync(
                catalogRepository,
                artistId,
                cancellationToken);

            loaded.Aggregate.TrackMetadataFetched(artistId, fetched);

            await loaded.Aggregate.SaveAsync(
                catalogRepository,
                loaded.Stream,
                CommandId.For($"MusicCatalogLookupCompleted:{command.LookupId.StableValue}:{version}"),
                cancellationToken);
        }
    }
}
