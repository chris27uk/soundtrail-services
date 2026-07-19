using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested;

public sealed class OnKnownMusicDataRequestedHandler(
    IWorkPlanner planner,
    IEventStreamRepository<CatalogWorkId> repository) : IHandler<RequestKnownMusicDataCommand>
{
    public async Task Handle(RequestKnownMusicDataCommand request, CancellationToken cancellationToken = default)
    {
        var context = new DiscoveryHistory.SearchRequestContext(request.TrustLevel, request.RiskScore, request.RequestedAt, request.CorrelationId);
        var streamId = CatalogWorkId.From(request.Operation);
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, context, cancellationToken);
        
        scope.Aggregate.Request(planner.Execute(request.Operation, WorkPlan()), request.Priority);
        
        scope.Save();
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
