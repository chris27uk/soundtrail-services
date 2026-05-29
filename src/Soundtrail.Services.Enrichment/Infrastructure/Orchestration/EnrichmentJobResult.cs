using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Jobs;

public sealed record EnrichmentJobResult(
    EnrichmentOutcome Outcome,
    TrackMapping? Mapping = null,
    ResolutionDemand? UpdatedDemand = null,
    DateTimeOffset? RetryAt = null);
