namespace Soundtrail.Domain.Catalog
{
    public sealed class Track(TrackId trackId)
    {
        public TrackId TrackId { get; } = trackId;

        public string Title { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string? AlbumTitle { get; set; }

        public string? AlbumId { get; set; }

        public int? DurationMs { get; set; }

        public string? Isrc { get; set; }

        public string? Mbid { get; set; }

        public DateOnly? ReleaseDate { get; set; }

        public string? ArtworkUrl { get; set; }

        public bool StreamingLocationsRequired { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Dictionary<string, StreamingLocation> ProviderReferences { get; } = new(StringComparer.Ordinal);

        public HashSet<string> FailedProviders { get; } = new(StringComparer.Ordinal);
    }
}
