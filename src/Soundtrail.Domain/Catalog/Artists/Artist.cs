namespace Soundtrail.Domain.Catalog.Artists
{
    public class Artist
    {
        public ArtistId Id { get; init; }
        public ArtistName Name { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }
    }
}
