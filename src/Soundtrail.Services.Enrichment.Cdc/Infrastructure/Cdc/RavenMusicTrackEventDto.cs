namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc
{
    internal sealed class RavenMusicTrackEventDto
    {
        public string Type { get; set; } = string.Empty;

        public string SourceProvider { get; set; } = string.Empty;

        public DateTimeOffset ObservedAt { get; set; }

        public string? MusicCatalogId { get; set; }

        public string? Priority { get; set; }

        public string? CorrelationId { get; set; }

        public string? LookupMode { get; set; }

        public string? Isrc { get; set; }

        public string? Title { get; set; }

        public string? Artist { get; set; }
        
        public string? ALbum { get; set; }
    }
}
