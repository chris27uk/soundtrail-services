using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

public static class WorkerCommandFactory
{
    public static IMessage Create(
        DispatchLookupWork request,
        LookupAttempt attempt) =>
        attempt switch
        {
            LookupAttempt.MusicbrainzSearchCatalogItems(var searchCriteria, var priority) =>
                new LookupMusicbrainzSearchResultsMessage(
                    CreateCommandId(request, $"lookup:musicbrainz-search:{searchCriteria.NormalisedIdentifier}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    searchCriteria),
            LookupAttempt.MusicbrainzArtistAlbums(var artistId, var priority) =>
                new LookupMusicbrainzArtistAlbumsMessage(
                    CreateCommandId(request, $"lookup:musicbrainz-artist-albums:{artistId.StableValue}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    artistId),
            LookupAttempt.MusicbrainzArtistTracks(var artistId, var priority) =>
                new LookupMusicbrainzArtistTracksMessage(
                    CreateCommandId(request, $"lookup:musicbrainz-artist-tracks:{artistId.StableValue}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    artistId),
            LookupAttempt.MusicbrainzAlbumTracks(var albumId, var priority) =>
                new LookupMusicbrainzAlbumTracksMessage(
                    CreateCommandId(request, $"lookup:musicbrainz-album-tracks:{albumId.StableValue}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    albumId),
            LookupAttempt.StreamingLocationByIsrc(var trackId, var provider, var priority) =>
                new LookupStreamingLocationByIsrcMessage(
                    CreateCommandId(request, $"lookup:streaming-isrc:{provider.Value}:{trackId.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    trackId,
                    provider),
            LookupAttempt.StreamingLocationByTrackMetadata(var trackId, var provider, var priority) =>
                new LookupStreamingLocationByTrackMetadataMessage(
                    CreateCommandId(request, $"lookup:streaming-metadata:{provider.Value}:{trackId.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    trackId,
                    provider),
            LookupAttempt.PlaylistTracksByProvider(var playlistId, var provider, var priority) =>
                new LookupPlaylistTracksByProviderMessage(
                    CreateCommandId(request, $"lookup:playlist:{provider.Value}:{playlistId.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    playlistId,
                    provider),
            _ => throw new InvalidOperationException(
                $"Unsupported lookup attempt '{attempt.GetType().Name}'.")
        };

    private static MessageId CreateCommandId(DispatchLookupWork request, string suffix) =>
        MessageId.DeterministicWithPrefix(request.Id.Value, suffix);
}
