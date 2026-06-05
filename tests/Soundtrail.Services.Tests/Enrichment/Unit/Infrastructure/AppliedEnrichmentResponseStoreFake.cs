using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

public sealed class AppliedEnrichmentResponseStoreFake : IAppliedEnrichmentResponseStore
{
    private readonly HashSet<string> appliedCommandIds = [];

    public IReadOnlyCollection<string> AppliedCommandIds => appliedCommandIds;

    public Task<bool> HasAppliedAsync(CommandId commandId, CancellationToken cancellationToken) =>
        Task.FromResult(appliedCommandIds.Contains(commandId.Value));

    public Task MarkAppliedAsync(CommandId commandId, CancellationToken cancellationToken)
    {
        appliedCommandIds.Add(commandId.Value);
        return Task.CompletedTask;
    }
}
