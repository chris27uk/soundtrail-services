namespace Soundtrail.Services.EnrichmentWorker.Jobs;

public enum EnrichmentOutcome
{
    Resolved = 0,
    PartiallyResolved = 1,
    NotFound = 2,
    RetryLater = 3,
    ProviderBudgetExceeded = 4,
    ProviderUnavailable = 5,
    Rejected = 6,
    Failed = 7
}
