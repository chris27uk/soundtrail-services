using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Scheduling;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Support;

public sealed class DiscoveryBacklogLookupPlanner
{
    public PlannedLookupWork? Plan(
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        DateTimeOffset now,
        LocalMusicTrackSearchResult? localTrack,
        CorrelationId? correlationId = null)
    {
        if (localTrack?.IsPlayable == true)
        {
            return null;
        }

        var searchTerm = localTrack is not null && localTrack.CanCreateSearchTerm()
            ? localTrack.ToSearchTerm()
            : null;
        if (!string.IsNullOrWhiteSpace(localTrack?.Isrc) && searchTerm is not null)
        {
            return new PlannedLookupWork(
                new LookupStreamingLocationsCommand(
                    CommandId.For($"LookupStreamingLocations:{musicCatalogId.Value}"),
                    musicCatalogId,
                    priority,
                    now,
                    correlationId ?? CorrelationId.New(),
                    searchTerm,
                    ToHierarchy(localTrack)),
                ProviderName.Odesli);
        }

        if (searchTerm is null)
        {
            return null;
        }

        return new PlannedLookupWork(
            new LookupTrackMetadataCommand(
                CommandId.For($"LookupTrackMetadata:{musicCatalogId.Value}"),
                musicCatalogId,
                priority,
                now,
                correlationId ?? CorrelationId.New(),
                searchTerm,
                ToHierarchy(localTrack)),
            ProviderName.MusicBrainz);
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult? localTrack) =>
        localTrack?.ArtistId is null && localTrack?.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack?.ArtistId, localTrack?.AlbumId);
}
