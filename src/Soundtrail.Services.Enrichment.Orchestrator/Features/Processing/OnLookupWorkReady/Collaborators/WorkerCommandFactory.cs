using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

public static class WorkerCommandFactory
{
    public static ICommand Create(
        DispatchLookupWork request,
        PlannedLookup lookup) =>
        lookup switch
        {
            PlannedLookup.MusicbrainzSearch(var searchCriteria, var priority) =>
                new LookupMusicbrainzSearchResultsCommand(
                    ChildId(request, "musicbrainz-search"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    searchCriteria),
            PlannedLookup.MusicbrainzArtistAlbums(var artistId, var priority) =>
                new LookupMusicbrainzArtistAlbumsCommand(
                    ChildId(request, "musicbrainz-artist-albums"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    artistId),
            PlannedLookup.MusicbrainzArtistTracks(var artistId, var priority) =>
                new LookupMusicbrainzArtistTracksCommand(
                    ChildId(request, "musicbrainz-artist-tracks"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    artistId),
            PlannedLookup.MusicbrainzAlbumTracks(var albumId, var priority) =>
                new LookupMusicbrainzAlbumTracksCommand(
                    ChildId(request, "musicbrainz-album-tracks"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    albumId),
            PlannedLookup.StreamingLocationByIsrc(var trackId, var provider, var priority) =>
                new LookupStreamingLocationByIsrcCommand(
                    ChildId(request, $"streaming-isrc:{provider.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    trackId,
                    provider),
            PlannedLookup.StreamingLocationByTrackMetadata(var trackId, var provider, var priority) =>
                new LookupStreamingLocationByTrackMetadataCommand(
                    ChildId(request, $"streaming-metadata:{provider.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    trackId,
                    provider),
            PlannedLookup.PlaylistTracksByProvider(var playlistId, var provider, var priority) =>
                new LookupPlaylistTracksByProviderCommand(
                    ChildId(request, $"playlist:{provider.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    playlistId,
                    provider),
            _ => throw new InvalidOperationException(
                $"Unsupported planned lookup '{lookup.GetType().Name}'.")
        };

    private static CommandId ChildId(DispatchLookupWork request, string suffix) =>
        CommandId.For($"{request.CommandId.Value}:{suffix}");
}
