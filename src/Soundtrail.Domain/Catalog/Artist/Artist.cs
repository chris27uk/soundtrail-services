namespace Soundtrail.Domain.Catalog
{
    public record Artist
    {
        public ArtistId Id { get; init; }
        public ArtistName Name { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }
    }
}
