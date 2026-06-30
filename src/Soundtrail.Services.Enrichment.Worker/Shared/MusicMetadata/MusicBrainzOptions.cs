namespace Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

public sealed class MusicBrainzOptions
{
    public const string SectionName = "MusicBrainz";

    public string BaseUrl { get; init; } = "https://musicbrainz.org";

    public string UserAgent { get; init; } = "Soundtrail.Services/1.0 ( chris@localhost )";
}
