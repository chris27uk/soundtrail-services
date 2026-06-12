using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features;

public sealed record LookupExecutionResult(EnrichmentResponse? Response)
{
    public static LookupExecutionResult Completed(EnrichmentResponse response) => new(response);

    public static LookupExecutionResult Duplicate() => new((EnrichmentResponse?)null);
}
