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
                    MessageId.For($"lookup:musicbrainz-search:{searchCriteria.NormalisedIdentifier}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    searchCriteria),
            LookupAttempt.MusicbrainzArtistAlbums(var artistId, var priority) =>
                new LookupMusicbrainzArtistAlbumsMessage(
                    MessageId.For($"lookup:musicbrainz-artist-albums:{artistId.StableValue}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    artistId),
            LookupAttempt.MusicbrainzArtistTracks(var artistId, var priority) =>
                new LookupMusicbrainzArtistTracksMessage(
                    MessageId.For($"lookup:musicbrainz-artist-tracks:{artistId.StableValue}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    artistId),
            LookupAttempt.MusicbrainzAlbumTracks(var albumId, var priority) =>
                new LookupMusicbrainzAlbumTracksMessage(
                    MessageId.For($"lookup:musicbrainz-album-tracks:{albumId.StableValue}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    albumId),
            LookupAttempt.StreamingLocationByIsrc(var trackId, var provider, var priority) =>
                new LookupStreamingLocationByIsrcMessage(
                    MessageId.For($"lookup:streaming-isrc:{provider.Value}:{trackId.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    trackId,
                    provider),
            LookupAttempt.StreamingLocationByTrackMetadata(var trackId, var provider, var priority) =>
                new LookupStreamingLocationByTrackMetadataMessage(
                    MessageId.For($"lookup:streaming-metadata:{provider.Value}:{trackId.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    trackId,
                    provider),
            LookupAttempt.PlaylistTracksByProvider(var playlistId, var provider, var priority) =>
                new LookupPlaylistTracksByProviderMessage(
                    MessageId.For($"lookup:playlist:{provider.Value}:{playlistId.Value}"),
                    request.CorrelationId,
                    request.CreatedAt,
                    priority,
                    playlistId,
                    provider),
            _ => throw new InvalidOperationException(
                $"Unsupported lookup attempt '{attempt.GetType().Name}'.")
        };
}
