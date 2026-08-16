namespace Soundtrail.Services.Enrichment.Worker.Shared.MusicBrainzDumpFreshness;

public sealed class MusicBrainzDumpFreshnessOptions
{
    public const string SectionName = "MusicBrainzDumpFreshness";

    public TimeSpan FreshWithin { get; set; } = TimeSpan.FromDays(30);
}
