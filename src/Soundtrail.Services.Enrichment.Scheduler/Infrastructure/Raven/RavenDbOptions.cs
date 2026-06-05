namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;

public sealed class RavenDbOptions
{
    public const string SectionName = "RavenDb";

    public string[] Urls { get; init; } = [];

    public string Database { get; init; } = "soundtrail";
}
