using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Scheduling;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Support;

public sealed class DiscoveryBacklogLookupPlanner
{
    internal PlannedLookupWork? Plan(
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        DateTimeOffset now,
        LocalMusicTrackSearchResult? localTrack)
    {
        if (localTrack?.IsPlayable == true)
        {
            return null;
        }

        var searchTerm = localTrack?.GetSearchTerm();
        if (!string.IsNullOrWhiteSpace(localTrack?.Isrc) && searchTerm is not null)
        {
            return new PlannedLookupWork(
                new ResolvePlaybackReferencesCommand(
                    CommandId.For($"ResolvePlaybackReferences:{musicCatalogId.Value}"),
                    musicCatalogId,
                    priority,
                    now,
                    CorrelationId.New(),
                    searchTerm,
                    ToHierarchy(localTrack)),
                ProviderName.Odesli);
        }

        if (searchTerm is null)
        {
            return null;
        }

        return new PlannedLookupWork(
            new LookupMusicMetadataCommand(
                CommandId.For($"LookupCanonicalMusicMetadata:{musicCatalogId.Value}"),
                musicCatalogId,
                priority,
                now,
                CorrelationId.New(),
                searchTerm,
                ToHierarchy(localTrack)),
            ProviderName.MusicBrainz);
    }

    private static CatalogTrackHierarchy? ToHierarchy(LocalMusicTrackSearchResult? localTrack) =>
        localTrack?.ArtistId is null && localTrack?.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(localTrack?.ArtistId, localTrack?.AlbumId);
}
