using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Features.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownMusicDataRequested;

public sealed class OnKnownMusicDataRequestedHandler(
    IWorkPlanner planner,
    IEventStreamRepository<CatalogWorkId> repository) : IHandler<RequestKnownMusicDataCommand>
{
    public async Task Handle(RequestKnownMusicDataCommand request, CancellationToken cancellationToken = default)
    {
        var context = new DiscoveryHistory.SearchRequestContext(request.TrustLevel, request.RiskScore, request.RequestedAt, request.CorrelationId);
        var streamId = CatalogWorkId.From(request.Operation);
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, context, cancellationToken);
        var rules = WorkPlan.Create(
            Rule.On<CatalogItemOperation.StreamingLocationForTrack>(x => [Work.EnrichTrackStreamingLocation(x.Id)]),
            Rule.On<CatalogItemOperation.ChildAlbumsForArtist>(x => [Work.DiscoverArtistAlbums(x.Id)]),
            Rule.On<CatalogItemOperation.ChildTracksForArtist>(x => [Work.DiscoverArtistTracks(x.Id)]),
            Rule.On<CatalogItemOperation.ChildTracksForAlbum>(x => [Work.DiscoverAlbumTracks(x.Id)]),
            Rule.On<CatalogItemOperation.ChildTracksForPlaylist>(x => [Work.DiscoverPlaylistTracks(x.Id)]));
        scope.Aggregate.Request(planner.Execute(request.Operation, rules));
        scope.Save();
    }
}
