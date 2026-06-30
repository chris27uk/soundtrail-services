using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Pipeline;

public sealed class LookupTrackMetadataExecutionAdmissionDecorator(ILookupExecutionAdmissionPort executionAdmissionPort, IHandler<LookupTrackMetadataCommand> inner, ICommandBus bus) : IHandler<LookupTrackMetadataCommand>
{
    public async Task Handle(LookupTrackMetadataCommand command, CancellationToken cancellationToken = default)
    {
        var admission = await executionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(
                LookupSource.MusicBrainz,
                command.CommandId,
                command.CreatedAt),
            cancellationToken);

        if (admission.Status == LookupExecutionAdmissionStatus.Duplicate)
        {
            await bus.SendAsync(MusicCatalogLookupAttempted.Duplicate(
                command.CommandId,
                command.MusicCatalogId,
                LookupSource.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId,
                command.SearchCriteria), cancellationToken);
            return;
        }

        if (admission.Status == LookupExecutionAdmissionStatus.Deferred)
        {
            await bus.SendAsync(MusicCatalogLookupAttempted.Deferred(
                command.CommandId,
                command.MusicCatalogId,
                LookupSource.MusicBrainz,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId,
                admission.Reason,
                admission.RetryAt,
                admission.RetryAfterSecondsFrom(command.CreatedAt),
                command.SearchCriteria), cancellationToken);
            return;
        }

        try
        {
            await inner.Handle(command, cancellationToken);
            await executionAdmissionPort.CommitAsync(command.CommandId, cancellationToken);
        }
        catch
        {
            await executionAdmissionPort.ReleaseAsync(command.CommandId, cancellationToken);
            throw;
        }
    }
}
