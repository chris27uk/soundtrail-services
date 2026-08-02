using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.TypeRegistry.Registrations;

public sealed class DiscoveryRequestTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<RequestKnownMusicDataMessage, KnownMusicDataRequestedCommandDto>(
            toDto: message => new KnownMusicDataRequestedCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                ToDto(message.Priority),
                GetOperationKind(message.Operation),
                GetOperationValue(message.Operation),
                GetOperationItemKind(message.Operation),
                message.TrustLevel,
                message.RiskScore,
                message.RequestedAt),
            toDomainObject: dto => new RequestKnownMusicDataMessage(
                ParseOperation(dto.OperationKind, dto.OperationValue, dto.OperationItemKind),
                ParsePriority(dto.Priority),
                dto.TrustLevel,
                dto.RiskScore,
                dto.RequestedAt)
            {
                Id = MessageId.From(dto.CommandId),
                CorrelationId = CorrelationId.From(dto.CorrelationId)
            });

        registry.RegisterPair<RequestUnknownMusicDataMessage, UnknownMusicDataRequestedCommandDto>(
            toDto: message => new UnknownMusicDataRequestedCommandDto(
                message.Id.Value,
                message.CorrelationId.Value,
                message.RequestedAt,
                ToDto(message.Priority),
                message.SearchCriteria.Query,
                (int)message.SearchCriteria.SearchTypes,
                message.TrustLevel,
                message.RiskScore,
                message.RequestedAt),
            toDomainObject: dto => new RequestUnknownMusicDataMessage(
                new SearchCriteria(dto.Query, (SearchType)dto.SearchTypes),
                ParsePriority(dto.Priority),
                dto.TrustLevel,
                dto.RiskScore,
                dto.RequestedAt,
                MessageId.From(dto.CommandId),
                CorrelationId.From(dto.CorrelationId))
            {
                RequestedAt = dto.RequestedAt
            });
    }

    private static LookupPriorityBandDto ToDto(LookupPriorityBand priority) =>
        priority switch
        {
            LookupPriorityBand.Low => LookupPriorityBandDto.Low,
            LookupPriorityBand.High => LookupPriorityBandDto.High,
            _ => throw new InvalidOperationException($"Unsupported lookup priority '{priority}'.")
        };

    private static LookupPriorityBand ParsePriority(LookupPriorityBandDto priority) =>
        priority switch
        {
            LookupPriorityBandDto.Low => LookupPriorityBand.Low,
            LookupPriorityBandDto.High => LookupPriorityBand.High,
            _ => throw new InvalidOperationException($"Unsupported lookup priority DTO '{priority}'.")
        };

    private static string GetOperationKind(CatalogItemOperation operation) =>
        operation switch
        {
            CatalogItemOperation.ChildAlbumsForArtist => "child_albums_for_artist",
            CatalogItemOperation.ChildTracksForArtist => "child_tracks_for_artist",
            CatalogItemOperation.ChildTracksForAlbum => "child_tracks_for_album",
            CatalogItemOperation.ChildTracksForPlaylist => "child_tracks_for_playlist",
            CatalogItemOperation.StreamingLocationForTrack => "streaming_location_for_track",
            _ => throw new InvalidOperationException($"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };

    private static string GetOperationValue(CatalogItemOperation operation) =>
        operation switch
        {
            CatalogItemOperation.ChildAlbumsForArtist(var artistId) => artistId.Value,
            CatalogItemOperation.ChildTracksForArtist(var artistId) => artistId.Value,
            CatalogItemOperation.ChildTracksForAlbum(var albumId) => albumId.StableValue,
            CatalogItemOperation.ChildTracksForPlaylist(var playlistId) => playlistId.Value,
            CatalogItemOperation.StreamingLocationForTrack(var trackId) => trackId.Value,
            _ => throw new InvalidOperationException($"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };

    private static string GetOperationItemKind(CatalogItemOperation operation) =>
        operation switch
        {
            CatalogItemOperation.StreamingLocationForTrack => "track",
            CatalogItemOperation.ChildAlbumsForArtist => "artist",
            CatalogItemOperation.ChildTracksForArtist => "artist",
            CatalogItemOperation.ChildTracksForAlbum => "album",
            CatalogItemOperation.ChildTracksForPlaylist => "playlist",
            _ => throw new InvalidOperationException($"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };

    private static CatalogItemOperation ParseOperation(string kind, string value, string itemKind) =>
        kind switch
        {
            "streaming_location_for_track" when itemKind == "track" => new CatalogItemOperation.StreamingLocationForTrack(Domain.Catalog.Tracks.TrackId.From(value)),
            "child_albums_for_artist" when itemKind == "artist" => new CatalogItemOperation.ChildAlbumsForArtist(Domain.Catalog.Artists.ArtistId.From(value)),
            "child_tracks_for_artist" when itemKind == "artist" => new CatalogItemOperation.ChildTracksForArtist(Domain.Catalog.Artists.ArtistId.From(value)),
            "child_tracks_for_album" when itemKind == "album" => new CatalogItemOperation.ChildTracksForAlbum(Domain.Catalog.Albums.AlbumId.From(value)),
            "child_tracks_for_playlist" when itemKind == "playlist" => new CatalogItemOperation.ChildTracksForPlaylist(Domain.Catalog.Playlists.PlaylistId.FromPlaylistName(value)),
            _ => throw new InvalidOperationException($"Unsupported catalog item operation DTO '{kind}' with item kind '{itemKind}'.")
        };
}
