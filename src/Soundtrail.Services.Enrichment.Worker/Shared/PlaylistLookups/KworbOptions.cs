namespace Soundtrail.Services.Enrichment.Worker.Shared.PlaylistLookups;

public sealed class KworbOptions
{
    public const string SectionName = "Kworb";

    public string BaseUrl { get; init; } = "https://kworb.net";
}
