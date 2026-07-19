using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Domain.Discovery.Planning;

public static class LookupPlanningPolicy
{
    public static LookupPlan Build(DispatchLookupWork request) =>
        new(BuildLookups(request));

    private static IReadOnlyList<PlannedLookup> BuildLookups(DispatchLookupWork request) =>
        request.Target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) =>
            [
                new PlannedLookup.MusicbrainzSearch(criteria, request.Priority)
            ],
            EnrichmentTarget.KnownCatalogItemOperation(var operation) =>
                BuildKnownOperation(operation, request.Priority),
            _ => throw new InvalidOperationException(
                $"Unsupported enrichment target '{request.Target.GetType().Name}'.")
        };

    private static IReadOnlyList<PlannedLookup> BuildKnownOperation(
        CatalogItemOperation operation,
        LookupPriorityBand priority) =>
        operation switch
        {
            CatalogItemOperation.ChildAlbumsForArtist(var artistId) =>
            [
                new PlannedLookup.MusicbrainzArtistAlbums(artistId, priority)
            ],
            CatalogItemOperation.ChildTracksForArtist(var artistId) =>
            [
                new PlannedLookup.MusicbrainzArtistTracks(artistId, priority)
            ],
            CatalogItemOperation.ChildTracksForAlbum(var albumId) =>
            [
                new PlannedLookup.MusicbrainzAlbumTracks(albumId, priority)
            ],
            CatalogItemOperation.StreamingLocationForTrack(var trackId) =>
                ProviderName.All.SelectMany(provider => new PlannedLookup[]
                {
                    new PlannedLookup.StreamingLocationByIsrc(trackId, provider, priority),
                    new PlannedLookup.StreamingLocationByTrackMetadata(trackId, provider, priority)
                }).ToArray(),
            CatalogItemOperation.ChildTracksForPlaylist(var playlistId) =>
                ProviderName.All
                    .Select(provider => (PlannedLookup)new PlannedLookup.PlaylistTracksByProvider(
                        playlistId,
                        provider,
                        priority))
                    .ToArray(),
            _ => throw new InvalidOperationException(
                $"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };
}
