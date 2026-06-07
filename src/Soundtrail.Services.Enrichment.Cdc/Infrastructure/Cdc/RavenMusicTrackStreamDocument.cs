namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

internal sealed class RavenMusicTrackStreamDocument
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public int Version { get; set; }

    public List<string> AppliedCommandIds { get; set; } = [];

    public List<RavenMusicTrackEventDocument> Facts { get; set; } = [];
}

internal sealed class RavenMusicTrackEventDocument
{
    public string Type { get; set; } = string.Empty;

    public string SourceProvider { get; set; } = string.Empty;

    public DateTimeOffset ObservedAt { get; set; }

    public string? MusicCatalogId { get; set; }

    public string? Priority { get; set; }

    public string? CorrelationId { get; set; }
}
