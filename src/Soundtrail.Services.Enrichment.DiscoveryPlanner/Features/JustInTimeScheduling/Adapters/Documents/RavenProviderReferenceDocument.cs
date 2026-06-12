namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents
{
    public sealed class RavenProviderReferenceDocument
    {
        public string Provider { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string? ExternalId { get; set; }

        public string SourceProvider { get; set; } = string.Empty;
    }
}
