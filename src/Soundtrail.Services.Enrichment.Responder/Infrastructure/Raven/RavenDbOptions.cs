namespace Soundtrail.Services.Enrichment.Responder.Infrastructure.Raven;

public sealed class RavenDbOptions
{
    public const string SectionName = "RavenDb";

    public string[] Urls { get; init; } = [];

    public string Database { get; init; } = string.Empty;
}
