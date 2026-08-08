using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist
{
    internal sealed class InMemoryEventStreamRepository<TStreamId>(
        Func<IReadOnlyList<IDomainEvent>, CancellationToken, Task>? onAppend = null) : IEventStreamRepository<TStreamId>
        where TStreamId : IValueType
    {
        private readonly Dictionary<string, List<IDomainEvent>> eventsByStream = new(StringComparer.Ordinal);
        private readonly HashSet<string> operationIds = new(StringComparer.Ordinal);

        public IReadOnlyList<IDomainEvent> SavedEvents => eventsByStream.Values.SelectMany(static events => events).ToArray();

        public Task<LoadedEventStream<TStreamId>> LoadAsync(TStreamId streamId, CancellationToken cancellationToken)
        {
            var streamKey = streamId.StableValue;
            var events = this.eventsByStream.TryGetValue(streamKey, out var existing)
                ? existing.ToArray()
                : [];
            return Task.FromResult(new LoadedEventStream<TStreamId>(streamId, events.Length, events));
        }

        public async Task<AppendResult> AppendAsync(
            LoadedEventStream<TStreamId> stream,
            IReadOnlyList<IDomainEvent> events,
            OperationId? operationId,
            CancellationToken cancellationToken)
        {
            if (operationId is not null && !this.operationIds.Add(operationId.Value.StableValue))
            {
                return new AppendResult(false, stream.Version, [], AppendOutcome.DuplicateOperation);
            }

            var streamKey = stream.StreamId.StableValue;
            var existing = this.eventsByStream.GetValueOrDefault(streamKey);
            if (existing is null)
            {
                existing = [];
                this.eventsByStream[streamKey] = existing;
            }

            if (existing.Count != stream.Version)
            {
                return new AppendResult(false, existing.Count, [], AppendOutcome.VersionMismatch);
            }

            existing.AddRange(events);
            if (onAppend is not null && events.Count > 0)
            {
                await onAppend(events, cancellationToken);
            }

            return new AppendResult(true, existing.Count, events, AppendOutcome.Appended);
        }
    }
}
