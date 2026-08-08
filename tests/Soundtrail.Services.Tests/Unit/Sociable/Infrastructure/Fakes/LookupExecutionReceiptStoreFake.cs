using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class LookupExecutionReceiptStoreFake : ILookupExecutionReceiptStore
{
    public bool TryBeginResult { get; set; } = true;

    public List<MessageId> TryBeginCommandIds { get; } = [];

    public List<MessageId> CompletedCommandIds { get; } = [];

    public List<MessageId> ReleasedCommandIds { get; } = [];

    public Task<bool> TryBeginAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        TryBeginCommandIds.Add(messageId);
        return Task.FromResult(TryBeginResult);
    }

    public Task MarkCompletedAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        CompletedCommandIds.Add(messageId);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        ReleasedCommandIds.Add(messageId);
        return Task.CompletedTask;
    }
}
