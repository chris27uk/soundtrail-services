using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Extensions;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;

public sealed class LookupCompletedHandler(
    IEventStreamRepository<CatalogWorkId> repository,
    ICommandBus commandBus) : IHandler<CatalogLookupCompleted>
{
    public async Task Handle(IncomingMessage<CatalogLookupCompleted> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var lookupRequest = request.Result;
        var streamId = lookupRequest.StreamId();
        var historyContext = request.ToAggregateContext();
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, historyContext, cancellationToken);
        
        scope.Aggregate.ApplyLookupResult(lookupRequest);
        
        await scope.Aggregate.SaveAsync(cancellationToken);
        await PublishPlaylistTrackDiscoveryRequestsAsync(request, lookupRequest, cancellationToken);
        await PublishStreamingLocationDiscoveryRequestsAsync(request, lookupRequest, cancellationToken);
    }

    private async Task PublishPlaylistTrackDiscoveryRequestsAsync(
        CatalogLookupCompleted request,
        LookupResult lookupRequest,
        CancellationToken cancellationToken)
    {
        if (lookupRequest is not LookupResult.Succeeded succeeded)
        {
            return;
        }

        if (succeeded.Context.StreamId.StableValue.StartsWith("child_tracks_for_playlist:", StringComparison.Ordinal) is false)
        {
            return;
        }

        if (succeeded.Value is not LookedUpData.PlaylistTrackReferences playlistTrackReferences)
        {
            return;
        }

        foreach (var trackReference in playlistTrackReferences.Values)
        {
            var searchCriteria = new SearchCriteria(
                $"{trackReference.TrackTitle} {trackReference.ArtistName.Value}".Trim(),
                SearchType.Track);

            await commandBus.SendAsync(
                new RequestUnknownMusicDataMessage(
                    searchCriteria,
                    LookupPriorityBand.High,
                    100,
                    0,
                    request.RequestedAt,
                    CommandId: MessageId.For(
                        $"RequestUnknownMusicData:{succeeded.Context.StreamId.StableValue}:{searchCriteria.NormalisedIdentifier}"),
                    CorrelationId: request.CorrelationId),
                cancellationToken);
        }
    }

    private async Task PublishStreamingLocationDiscoveryRequestsAsync(
        CatalogLookupCompleted request,
        LookupResult lookupRequest,
        CancellationToken cancellationToken)
    {
        if (lookupRequest is not LookupResult.Succeeded succeeded)
        {
            return;
        }

        if (succeeded.Value is not LookedUpData.CatalogEntries catalogEntries)
        {
            return;
        }

        foreach (var track in catalogEntries.Values
                     .Select(static entry => entry.Item)
                     .OfType<CatalogItem.MusicTrack>())
        {
            await commandBus.SendAsync(
                new RequestKnownMusicDataMessage(
                    new CatalogItemOperation.StreamingLocationForTrack(track.Track.TrackId),
                    LookupPriorityBand.High,
                    100,
                    0,
                    request.RequestedAt)
                {
                    Id = MessageId.For(
                        $"RequestKnownMusicData:{succeeded.Context.StreamId.StableValue}:streaming:{track.Track.TrackId.Value}"),
                    CorrelationId = request.CorrelationId,
                    CreatedAt = request.RequestedAt
                },
                cancellationToken);
        }
    }
}
