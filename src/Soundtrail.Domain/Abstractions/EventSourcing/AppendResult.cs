namespace Soundtrail.Domain.Abstractions.EventSourcing;

public sealed record AppendResult(
    bool Appended,
    int Version,
    IReadOnlyList<IDomainEvent> Events,
    AppendOutcome Outcome);
