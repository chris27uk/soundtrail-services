using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Scheduling;

public sealed record PlannedLookupWork(
    IMusicCatalogLookupCommand Command,
    ProviderName Source);
