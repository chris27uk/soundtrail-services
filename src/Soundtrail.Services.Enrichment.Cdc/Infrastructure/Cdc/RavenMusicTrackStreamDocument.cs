namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

internal sealed class RavenMusicTrackStreamDocument
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public int Version { get; set; }

    public List<string> AppliedCommandIds { get; set; } = [];

    public List<RavenMusicTrackEventDocument> Events { get; set; } = [];
}
