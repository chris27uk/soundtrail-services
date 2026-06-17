namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;

public sealed class OdesliOptions
{
    public const string SectionName = "Odesli";

    public string BaseUrl { get; init; } = "https://api.song.link";

    public string UserCountry { get; init; } = "US";
}
