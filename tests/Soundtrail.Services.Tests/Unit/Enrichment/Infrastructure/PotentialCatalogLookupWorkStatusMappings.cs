using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure
{
    internal static class PotentialCatalogLookupWorkStatusMappings
    {
        public static CatalogDiscoveryWorkStatus ToDiscoveryStatus(this PotentialCatalogLookupWorkStatus status) =>
            status switch
            {
                PotentialCatalogLookupWorkStatus.Pending => CatalogDiscoveryWorkStatus.Pending,
                PotentialCatalogLookupWorkStatus.Ignored => CatalogDiscoveryWorkStatus.Ignored,
                PotentialCatalogLookupWorkStatus.Resolved => CatalogDiscoveryWorkStatus.Resolved,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };

        public static PotentialCatalogLookupWorkStatus ToLegacyStatus(this CatalogDiscoveryWorkStatus status) =>
            status switch
            {
                CatalogDiscoveryWorkStatus.Pending => PotentialCatalogLookupWorkStatus.Pending,
                CatalogDiscoveryWorkStatus.Ignored => PotentialCatalogLookupWorkStatus.Ignored,
                CatalogDiscoveryWorkStatus.Resolved => PotentialCatalogLookupWorkStatus.Resolved,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
    }
}