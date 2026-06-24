namespace Soundtrail.Contracts.Persistence;

public sealed class RavenProviderReferenceRecordDto
{
    public string Provider { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public string SourceProvider { get; set; } = string.Empty;
}
