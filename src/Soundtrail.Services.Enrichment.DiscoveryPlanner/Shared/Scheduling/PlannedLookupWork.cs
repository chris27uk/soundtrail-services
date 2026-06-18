using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Scheduling;

internal sealed record PlannedLookupWork(
    IMusicCatalogLookupCommand Command,
    ProviderName Source);
