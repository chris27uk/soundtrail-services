using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Domain.Discovery.Planning;

public static class LookupPlanningPolicy
{
    public static LookupPlan Build(DispatchLookupWork request) =>
        new(BuildIntents(request));

    private static IReadOnlyList<LookupIntent> BuildIntents(DispatchLookupWork request) =>
        request.Target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem(var criteria) =>
            [
                new LookupIntent.SearchCatalogItems(
                    criteria,
                    request.Priority,
                    [
                        new LookupAttempt.MusicbrainzSearchCatalogItems(criteria, request.Priority)
                    ])
            ],
            EnrichmentTarget.KnownCatalogItemOperation(var operation) =>
                BuildKnownOperation(operation, request.Priority),
            _ => throw new InvalidOperationException(
                $"Unsupported enrichment target '{request.Target.GetType().Name}'.")
        };

    private static IReadOnlyList<LookupIntent> BuildKnownOperation(
        CatalogItemOperation operation,
        LookupPriorityBand priority) =>
        operation switch
        {
            CatalogItemOperation.ChildAlbumsForArtist(var artistId) =>
            [
                new LookupIntent.ArtistAlbums(
                    artistId,
                    priority,
                    [
                        new LookupAttempt.MusicbrainzArtistAlbums(artistId, priority)
                    ])
            ],
            CatalogItemOperation.ChildTracksForArtist(var artistId) =>
            [
                new LookupIntent.ArtistTracks(
                    artistId,
                    priority,
                    [
                        new LookupAttempt.MusicbrainzArtistTracks(artistId, priority)
                    ])
            ],
            CatalogItemOperation.ChildTracksForAlbum(var albumId) =>
            [
                new LookupIntent.AlbumTracks(
                    albumId,
                    priority,
                    [
                        new LookupAttempt.MusicbrainzAlbumTracks(albumId, priority)
                    ])
            ],
            CatalogItemOperation.StreamingLocationForTrack(var trackId) =>
            [
                new LookupIntent.StreamingLocation(
                    trackId,
                    priority,
                    ProviderName.All.SelectMany(provider => new LookupAttempt[]
                    {
                        new LookupAttempt.StreamingLocationByIsrc(trackId, provider, priority),
                        new LookupAttempt.StreamingLocationByTrackMetadata(trackId, provider, priority)
                    }).ToArray())
            ],
            CatalogItemOperation.ChildTracksForPlaylist(var playlistId) =>
            [
                new LookupIntent.PlaylistTracks(
                    playlistId,
                    priority,
                    ProviderName.All
                    .Select(provider => (LookupAttempt)new LookupAttempt.PlaylistTracksByProvider(
                        playlistId,
                        provider,
                        priority))
                    .ToArray())
            ],
            _ => throw new InvalidOperationException(
                $"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };
}
