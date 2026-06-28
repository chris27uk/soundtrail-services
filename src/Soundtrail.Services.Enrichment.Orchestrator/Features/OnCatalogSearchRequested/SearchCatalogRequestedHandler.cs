using Soundtrail.Domain.Abstractions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class SearchCatalogRequestedHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    ICommandBus commandBus,
    ILocalMusicTrackSearch localMusicTrackSearch) : IHandler<SearchCatalogRequested>
{
    public async Task Handle(
        SearchCatalogRequested requested,
        CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(requested.SearchCriteria, cancellationToken);
        var followUp = await new MusicTrackSearchMatchCollection(matches)
            .DetermineFollowUpAsync(
                requested.SearchCriteria,
                requested.Playback,
                localMusicTrackSearch,
                cancellationToken);

        if (followUp.RequiresTrackMetadataLookup)
        {
            await commandBus.SendAsync(
                new RecordTrackMetadataLookupRequestedCommand(
                    requested.SearchCriteria,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.OccurredAt,
                    requested.CorrelationId)
                {
                    CommandId = CommandId.For(
                        $"RecordTrackMetadataLookupRequested:{DiscoveryQueryKey.StableValueFor(requested.SearchCriteria)}:{requested.OccurredAt:O}")
                },
                cancellationToken);

            return;
        }

        foreach (var lookup in followUp.StreamingLocationLookups)
        {
            await commandBus.SendAsync(
                new RecordCatalogSearchCandidateCommand(
                    requested.SearchCriteria,
                    lookup.MusicCatalogId,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.OccurredAt,
                    requested.CorrelationId)
                {
                    CommandId = CommandId.For(
                        $"RecordCatalogSearchCandidate:{DiscoveryQueryKey.StableValueFor(requested.SearchCriteria)}:{lookup.MusicCatalogId.Value}")
                },
                cancellationToken);
        }
    }
}
