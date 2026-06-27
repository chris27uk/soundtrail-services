namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

public sealed class CatalogProviderReferenceRecordDto
{
    public string Provider { get; set; } = string.Empty;

    public string ProviderEntityType { get; set; } = string.Empty;

    public string ProviderId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTimeOffset DiscoveredAt { get; set; }
}
