namespace Soundtrail.Domain.Enrichment.Responses;

public sealed record AlbumMetadata(
    string AlbumTitle,
    string ArtistName,
    string? SourceAlbumId = null,
    string? SourceArtistId = null,
    DateOnly? ReleaseDate = null);
