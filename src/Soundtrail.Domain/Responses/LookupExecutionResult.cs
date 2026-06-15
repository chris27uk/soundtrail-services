using Soundtrail.Domain.Responses;

namespace Soundtrail.Domain.Responses;

public sealed record LookupExecutionResult(bool Started, EnrichmentResponse? Response)
{
    public static LookupExecutionResult Completed(EnrichmentResponse response) => new(true, response);

    public static LookupExecutionResult Duplicate() => new(false, (EnrichmentResponse?)null);
}
