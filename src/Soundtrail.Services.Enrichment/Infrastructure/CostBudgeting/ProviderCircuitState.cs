namespace Soundtrail.Services.Enrichment.Models;

public sealed record ProviderCircuitState(
    ProviderName Provider,
    CircuitState State,
    int FailureCount,
    DateTimeOffset? LastFailureAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? HalfOpenAt,
    string? Reason);

public enum CircuitState
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}
