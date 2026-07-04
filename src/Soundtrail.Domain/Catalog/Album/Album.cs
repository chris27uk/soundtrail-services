namespace Soundtrail.Domain.Catalog
{
    public sealed class Album(
        AlbumId albumId,
        string? albumTitle,
        string? sourceAlbumId,
        DateOnly? releaseDate,
        string? artworkUrl,
        DateTimeOffset updatedAt)
    {
        public AlbumId AlbumId { get; } = albumId;

        public string? AlbumTitle { get; } = albumTitle;

        public string? SourceAlbumId { get; } = sourceAlbumId;

        public DateOnly? ReleaseDate { get; } = releaseDate;

        public string? ArtworkUrl { get; set; } = artworkUrl;

        public DateTimeOffset UpdatedAt { get; set; } = updatedAt;
    }
}
