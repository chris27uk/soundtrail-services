namespace Soundtrail.Domain.Responses;

public enum LookupExecutionOutcome
{
    Completed = 0,
    Deferred = 1,
    Duplicate = 2,
    Failed = 3
}
