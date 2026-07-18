using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Services.Enrichment.Orchestrator.Features.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnUnknownMusicDataRequested;

public sealed class OnUnknownMusicDataRequestedHandler(
    IWorkPlanner planner,
    ISearchForCandidates searchForCandidates,
    IEventStreamRepository<CatalogWorkId> repository) : IHandler<RequestUnknownMusicDataCommand>
{
    public async Task Handle(RequestUnknownMusicDataCommand request, CancellationToken cancellationToken = default)
    {
        var context = new DiscoveryHistory.SearchRequestContext(request.TrustLevel, request.RiskScore, request.RequestedAt, request.CorrelationId);
        var streamId = CatalogWorkId.From(request.SearchCriteria);

        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, context, cancellationToken);

        var search = new EnrichmentTarget.SearchForUnknownCatalogItem(request.SearchCriteria);
        var result = searchForCandidates.Search(search);

        if (result is CandidatesResult.None)
        {
            scope.Aggregate.Request([Work.SearchExternally(request.SearchCriteria)]);
            scope.Save();
            return;
        }

        var rules = WorkPlan.Create(
            Rule.On<CatalogItemId.Track>(x => [Work.EnrichTrackStreamingLocation(x.Id)]),
            Rule.On<CatalogItemId.Artist>(x => [Work.DiscoverArtistAlbums(x.Id), Work.DiscoverArtistTracks(x.Id)]),
            Rule.On<CatalogItemId.Album>(x => [Work.DiscoverAlbumTracks(x.Id)]),
            Rule.On<CatalogItemId.Playlist>(_ => []));

        var work = ((CandidatesResult.Results)result).CandidateList.Ids
            .SelectMany(candidate => planner.Execute(candidate, rules))
            .ToArray();

        scope.Aggregate.Request(work);
        scope.Save();
    }
}
