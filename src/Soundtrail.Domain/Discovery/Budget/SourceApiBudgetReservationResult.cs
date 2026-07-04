namespace Soundtrail.Domain.Discovery.Budget;

public sealed record SourceApiBudgetReservationResult(
    bool Accepted,
    DateTimeOffset? RetryAt,
    string Reason)
{
    public int? RetryAfterSecondsFrom(DateTimeOffset now) =>
        RetryAt is null
            ? null
            : Math.Max(1, (int)Math.Ceiling((RetryAt.Value - now).TotalSeconds));

    public static SourceApiBudgetReservationResult Reserved() => new(true, null, string.Empty);

    public static SourceApiBudgetReservationResult Deferred(DateTimeOffset retryAt, string reason) =>
        new(false, retryAt, reason);
}
