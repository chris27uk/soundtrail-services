namespace Soundtrail.Adapters.EventSourcing;

public sealed record AppendResult<TEvent>(
    bool Appended,
    int Version,
    IReadOnlyList<TEvent> Events,
    AppendOutcome Outcome);
