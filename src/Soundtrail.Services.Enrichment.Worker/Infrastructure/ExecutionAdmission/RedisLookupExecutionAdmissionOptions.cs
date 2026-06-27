namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;

public sealed class RedisLookupExecutionAdmissionOptions
{
    public const string SectionName = "LookupExecutionAdmission";

    public int ActiveLeaseSeconds { get; init; } = 300;

    public string KeyPrefix { get; init; } = "lookup-execution-admission";
}
