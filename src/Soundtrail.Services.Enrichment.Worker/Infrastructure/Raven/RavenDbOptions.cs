namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

public sealed class RavenDbOptions
{
    public const string SectionName = "RavenDb";

    public string[] Urls { get; init; } = [];

    public string Database { get; init; } = "soundtrail";
}
