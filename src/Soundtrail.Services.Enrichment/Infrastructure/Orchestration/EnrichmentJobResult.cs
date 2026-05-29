using Soundtrail.Services.Enrichment.Features.LocalCache;

namespace Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

public sealed record EnrichmentJobResult(
    EnrichmentOutcome Outcome,
    TrackMapping? Mapping = null,
    ResolutionDemand? UpdatedDemand = null,
    DateTimeOffset? RetryAt = null);
