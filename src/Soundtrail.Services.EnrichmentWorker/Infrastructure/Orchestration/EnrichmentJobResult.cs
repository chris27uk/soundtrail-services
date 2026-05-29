using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Jobs;

public sealed record EnrichmentJobResult(
    EnrichmentOutcome Outcome,
    TrackMapping? Mapping = null,
    ResolutionDemand? UpdatedDemand = null,
    DateTimeOffset? RetryAt = null);
