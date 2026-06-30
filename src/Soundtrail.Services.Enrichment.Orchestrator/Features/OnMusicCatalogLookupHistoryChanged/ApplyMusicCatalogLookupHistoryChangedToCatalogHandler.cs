using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged;

public sealed class ApplyMusicCatalogLookupHistoryChangedToCatalogHandler(
    IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> catalogRepository) : IHandler<MusicCatalogLookupHistoryChangedCommand>
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

            var loaded = await MusicTrack.LoadAsync(
                catalogRepository,
                completed.MusicCatalogId,
                cancellationToken);

            loaded.Aggregate.MetadataFetched(
                new MusicCatalogMetadataFetched(
                    CommandId.For($"MusicCatalogLookupCompleted:{command.LookupId.StableValue}:{version}"),
                    completed.MusicCatalogId,
                    completed.SourceProvider,
                    completed.Priority,
                    completed.CompletedAt,
                    completed.Metadata,
                    completed.References,
                    completed.FailedProviders,
                    completed.Hierarchy,
                    CorrelationId.From($"lookup:{command.LookupId.StableValue}:{version}")));

            await loaded.Aggregate.SaveAsync(
                catalogRepository,
                loaded.Stream,
                CommandId.For($"MusicCatalogLookupCompleted:{command.LookupId.StableValue}:{version}"),
                cancellationToken);
        }
    }
}
