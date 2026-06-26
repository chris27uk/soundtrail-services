using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;

public sealed class LookupStreamingLocationsExecutionAdmissionDecorator(
    ILookupExecutionAdmissionPort executionAdmissionPort,
    ILookupStreamingLocationsHandler inner) : ILookupStreamingLocationsHandler
{
    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupStreamingLocationsCommand command,
        CancellationToken cancellationToken = default)
    {
        var admission = await executionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(
                command.TargetProvider,
                command.CommandId,
                command.CreatedAt),
            cancellationToken);

        switch (admission.Status)
        {
            case LookupExecutionAdmissionStatus.Duplicate:
                return MusicCatalogLookupAttempted.Duplicate(
                    command.CommandId,
                    command.MusicCatalogId,
                    command.TargetProvider,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId);
            case LookupExecutionAdmissionStatus.Deferred:
                return MusicCatalogLookupAttempted.Deferred(
                    command.CommandId,
                    command.MusicCatalogId,
                    command.TargetProvider,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId,
                    admission.Reason,
                    admission.RetryAt,
                    admission.RetryAfterSecondsFrom(command.CreatedAt));
        }

        try
        {
            var result = await inner.Handle(command, cancellationToken);
            if (result.Outcome.Status is MusicCatalogLookupOutcomeStatus.Completed or MusicCatalogLookupOutcomeStatus.Failed)
            {
                await executionAdmissionPort.CommitAsync(command.CommandId, cancellationToken);
            }
            else
            {
                await executionAdmissionPort.ReleaseAsync(command.CommandId, cancellationToken);
            }

            return result;
        }
        catch
        {
            await executionAdmissionPort.ReleaseAsync(command.CommandId, cancellationToken);
            throw;
        }
    }
}
