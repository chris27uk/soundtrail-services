namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven.Documents;

internal sealed class RavenProviderSnapshotDocument
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public static string GetDocumentId(string stableId, string provider) =>
        $"provider-snapshots/{stableId}/{provider}";
}
