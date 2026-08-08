using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator.Shared;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested;

public sealed class OnKnownMusicDataRequestedHandler(
    IWorkPlanner planner,
    IEventStreamRepository<CatalogWorkId> repository) : IHandler<RequestKnownMusicDataMessage>
{
    public async Task Handle(IncomingMessage<RequestKnownMusicDataMessage> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        using var handlerActivity = MessageTelemetry.StartHandlerActivity(request, "known-music-data-requested");
        MessageTelemetry.EnrichCurrentActivity(request, "known-music-data-requested");
        MessageTelemetry.AddCurrentEvent("known-music-data-requested.received");

        var aggregateContext = request.ToAggregateContext();
        var streamId = CatalogWorkId.From(request.Operation);
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, aggregateContext, cancellationToken);
        
        scope.Aggregate.Request(planner.Execute(request.Operation, WorkPlan()), request.Priority);
        MessageTelemetry.AddCurrentEvent("known-music-data-requested.work-requested-appended");
        
        scope.Save();
        MessageTelemetry.AddCurrentEvent("known-music-data-requested.saved");
    }

    private static WorkPlan WorkPlan()
    {
        return Shared.RequestedWork.WorkPlan.Create(
            Rule.WhenStreamingLocationForTrack()
                .Then(track => Work.EnrichTrackStreamingLocation(track.Id)),
            Rule.WhenChildAlbumsForArtist()
                .Then(artist => Work.DiscoverArtistAlbums(artist.Id)),
            Rule.WhenChildTracksForArtist()
                .Then(artist => Work.DiscoverArtistTracks(artist.Id)),
            Rule.WhenChildTracksForAlbum()
                .Then(album => Work.DiscoverAlbumTracks(album.Id)),
            Rule.WhenChildTracksForPlaylist()
                .Then(playlist => Work.DiscoverPlaylistTracks(playlist.Id)));
    }
}
