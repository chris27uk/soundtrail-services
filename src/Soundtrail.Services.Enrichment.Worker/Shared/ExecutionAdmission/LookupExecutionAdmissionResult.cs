namespace Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

public sealed record LookupExecutionAdmissionResult(
    LookupExecutionAdmissionStatus Status,
    DateTimeOffset? RetryAt,
    string Reason)
{
    public int? RetryAfterSecondsFrom(DateTimeOffset now) =>
        RetryAt is null
            ? null
            : Math.Max(1, (int)Math.Ceiling((RetryAt.Value - now).TotalSeconds));

    public static LookupExecutionAdmissionResult Acquired() =>
        new(LookupExecutionAdmissionStatus.Acquired, null, string.Empty);

    public static LookupExecutionAdmissionResult Deferred(
        DateTimeOffset retryAt,
        string reason) =>
        new(LookupExecutionAdmissionStatus.Deferred, retryAt, reason);

    public static LookupExecutionAdmissionResult Duplicate() =>
        new(LookupExecutionAdmissionStatus.Duplicate, null, string.Empty);
}
