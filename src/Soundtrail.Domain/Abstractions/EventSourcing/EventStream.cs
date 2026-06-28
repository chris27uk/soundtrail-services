namespace Soundtrail.Domain.Abstractions.EventSourcing;

public sealed record EventStream<TEvent>(int Version, IReadOnlyList<TEvent> Events);
