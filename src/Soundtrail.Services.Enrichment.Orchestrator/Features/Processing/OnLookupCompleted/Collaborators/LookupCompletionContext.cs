using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Collaborators;

internal sealed record LookupCompletionContext(EnrichmentTarget Target, LookupPriorityBand Priority, CorrelationId CorrelationId);
