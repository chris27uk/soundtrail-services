namespace Soundtrail.Domain.Catalog.Albums
{
    public sealed class Album(
        AlbumId albumId,
        string? albumTitle,
        IEnumerable<SourceSystemId>? sourceSystemIds,
        DateOnly? releaseDate,
        string? artworkUrl,
        DateTimeOffset updatedAt)
    {
        public AlbumId AlbumId { get; } = albumId;

        public string? AlbumTitle { get; } = albumTitle;

        public HashSet<SourceSystemId> SourceSystemIds { get; } = sourceSystemIds is null
            ? []
            : new HashSet<SourceSystemId>(sourceSystemIds);

        public DateOnly? ReleaseDate { get; } = releaseDate;

        public string? ArtworkUrl { get; set; } = artworkUrl;

        public DateTimeOffset UpdatedAt { get; set; } = updatedAt;
    }
}
