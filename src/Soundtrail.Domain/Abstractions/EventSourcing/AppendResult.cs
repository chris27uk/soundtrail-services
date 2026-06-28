namespace Soundtrail.Domain.Abstractions.EventSourcing;

public sealed record AppendResult<TEvent>(
    bool Appended,
    int Version,
    IReadOnlyList<TEvent> Events,
    AppendOutcome Outcome);
