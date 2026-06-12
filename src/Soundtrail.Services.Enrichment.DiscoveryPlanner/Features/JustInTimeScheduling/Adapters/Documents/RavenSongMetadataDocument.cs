namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents
{
    public sealed class RavenSongMetadataDocument
    {
        public string Title { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        public string? Isrc { get; set; }

        public string? Mbid { get; set; }

        public int? DurationMs { get; set; }
    }
}