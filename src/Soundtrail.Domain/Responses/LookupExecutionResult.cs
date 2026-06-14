using Soundtrail.Domain.Responses;

namespace Soundtrail.Domain.Responses;

public sealed record LookupExecutionResult(EnrichmentResponse? Response)
{
    public static LookupExecutionResult Completed(EnrichmentResponse response) => new(response);

    public static LookupExecutionResult Duplicate() => new((EnrichmentResponse?)null);
}
