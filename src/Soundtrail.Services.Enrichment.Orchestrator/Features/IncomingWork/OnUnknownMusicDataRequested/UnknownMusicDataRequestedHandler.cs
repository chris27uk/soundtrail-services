using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested;

public sealed class OnUnknownMusicDataRequestedHandler(
    IWorkPlanner planner,
    ISearchForCandidates searchForCandidates,
    IEventStreamRepository<CatalogWorkId> repository) : IHandler<RequestUnknownMusicDataCommand>
{
    public async Task Handle(RequestUnknownMusicDataCommand request, CancellationToken cancellationToken = default)
    {
        var context = new DiscoveryHistory.SearchRequestContext(request.TrustLevel, request.RiskScore, request.RequestedAt, request.CorrelationId);
        var streamId = CatalogWorkId.From(request.SearchCriteria);
        var search = new EnrichmentTarget.SearchForUnknownCatalogItem(request.SearchCriteria);
        var result = searchForCandidates.Search(search);
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, context, cancellationToken);

        if (result is CandidatesResult.None)
        {
            scope.Aggregate.Request([Work.SearchExternally(request.SearchCriteria)], request.Priority);
            scope.Save();
            return;
        }

        var work = ((CandidatesResult.Results)result).CandidateList
            .AsCandidateIds()
            .SelectMany(candidate => planner.Execute(candidate, WorkPlan()))
            .ToArray();

        scope.Aggregate.Request(work, request.Priority);
        scope.Save();
    }

    private static WorkPlan WorkPlan() => Shared.RequestedWork.WorkPlan.Create(
    [
        Rule.WhenTrack()
            .Then(track => Work.EnrichTrackStreamingLocation(track.Id)),
        Rule.WhenArtist()
            .Then(artist => Work.DiscoverArtistAlbums(artist.Id))
            .And(artist => Work.DiscoverArtistTracks(artist.Id)),
        Rule.WhenAlbum()
            .Then(album => Work.DiscoverAlbumTracks(album.Id)),
        Rule.WhenPlaylist()
            .ThenNone()
    ]);
}
