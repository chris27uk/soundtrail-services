namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogStreamingLocationRecordDto
{
    public string Provider { get; set; } = string.Empty;

    public string? ExternalId { get; set; }

    public string Url { get; set; } = string.Empty;
}
