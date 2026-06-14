namespace Soundtrail.Domain.Discovery;

public enum DiscoveryLifecycleStatus
{
    Requested = 0,
    Planned = 1,
    Deferred = 2,
    InProgress = 3,
    Completed = 4,
    Failed = 5,
    Rejected = 6
}
