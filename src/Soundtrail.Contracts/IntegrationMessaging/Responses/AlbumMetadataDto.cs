namespace Soundtrail.Contracts.IntegrationMessaging.Responses;

public sealed record AlbumMetadataDto(
    string AlbumTitle,
    string ArtistName,
    string? SourceAlbumId = null,
    string? SourceArtistId = null,
    DateOnly? ReleaseDate = null);
