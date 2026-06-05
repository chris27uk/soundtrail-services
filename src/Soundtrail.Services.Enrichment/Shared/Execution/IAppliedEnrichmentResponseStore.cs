using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Execution;

public interface IAppliedEnrichmentResponseStore
{
    Task<bool> HasAppliedAsync(CommandId commandId, CancellationToken cancellationToken);

    Task MarkAppliedAsync(CommandId commandId, CancellationToken cancellationToken);
}
