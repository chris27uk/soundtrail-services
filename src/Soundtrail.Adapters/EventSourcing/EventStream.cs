namespace Soundtrail.Adapters.EventSourcing;

public sealed record EventStream<TEvent>(int Version, IReadOnlyList<TEvent> Events);
