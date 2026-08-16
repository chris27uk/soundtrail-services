namespace Soundtrail.Adapters.MusicBrainzDumpFreshness;

public sealed class MusicBrainzDumpFreshnessOptions
{
    public const string SectionName = "MusicBrainzDumpFreshness";

    public TimeSpan FreshWithin { get; set; } = TimeSpan.FromDays(30);
}
