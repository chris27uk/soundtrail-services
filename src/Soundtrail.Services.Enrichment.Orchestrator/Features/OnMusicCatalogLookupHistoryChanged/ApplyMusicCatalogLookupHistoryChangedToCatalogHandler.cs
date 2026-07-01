using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged;

public sealed class ApplyMusicCatalogLookupHistoryChangedToCatalogHandler(
    IEventStreamRepository<MusicCatalogLookupId, IDomainEvent> historyRepository,
    IEventStreamRepository<ArtistId, IDomainEvent> catalogRepository) : IHandler<MusicCatalogLookupHistoryChangedCommand>
{
    public async Task Handle(
        MusicCatalogLookupHistoryChangedCommand command,
        CancellationToken cancellationToken = default)
    {
        var lookupHistory = await historyRepository.LoadAsync(command.LookupId, cancellationToken);
        var resolvedArtistsByVersion = ResolveArtistsByVersion(lookupHistory.Events);

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

            var artistId = resolvedArtistsByVersion.TryGetValue(version, out var resolvedArtistId)
                ? resolvedArtistId
                : throw new InvalidOperationException(
                    $"Unable to resolve artist id for lookup history event '{command.LookupId.StableValue}:{version}'.");
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

    private static Dictionary<int, ArtistId> ResolveArtistsByVersion(IReadOnlyList<IDomainEvent> events)
    {
        var resolved = new Dictionary<int, ArtistId>();
        ArtistId? lastResolvedArtistId = null;

        for (var index = 0; index < events.Count; index++)
        {
            if (events[index] is not MusicCatalogLookupCompleted completed)
            {
                continue;
            }

            var fetched = new MusicCatalogMetadataFetched(
                CommandId.For($"MusicCatalogLookupHistory:{completed.MusicCatalogId.Value}:{index + 1}"),
                completed.MusicCatalogId,
                completed.SourceProvider,
                completed.Priority,
                completed.CompletedAt,
                completed.Metadata,
                completed.References,
                completed.FailedProviders,
                completed.Hierarchy,
                CorrelationId.From($"lookup-history:{completed.MusicCatalogId.Value}:{index + 1}"));

            lastResolvedArtistId = ArtistCatalogIdentity.ResolveArtistIdOrNull(fetched) ?? lastResolvedArtistId;
            if (lastResolvedArtistId is not null)
            {
                resolved[index + 1] = lastResolvedArtistId.Value;
            }
        }

        return resolved;
    }
}
