namespace Soundtrail.Domain.Discovery;

public sealed record TryReserveSourceApiBudgetWindowResult(
    bool Reserved,
    DateTimeOffset RetryAt)
{
    public static TryReserveSourceApiBudgetWindowResult Success(DateTimeOffset retryAt) => new(true, retryAt);

    public static TryReserveSourceApiBudgetWindowResult Rejected(DateTimeOffset retryAt) => new(false, retryAt);
}
