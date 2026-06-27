using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;

public sealed class LookupStreamingLocationsExecutionAdmissionDecorator(
    ILookupExecutionAdmissionPort executionAdmissionPort,
    IHandler<LookupStreamingLocationsCommand> inner,
    ICommandBus bus) : IHandler<LookupStreamingLocationsCommand>
{
    public async Task Handle(
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
                await bus.SendAsync(MusicCatalogLookupAttempted.Duplicate(
                    command.CommandId,
                    command.MusicCatalogId,
                    command.TargetProvider,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId), cancellationToken);
                return;
            
            case LookupExecutionAdmissionStatus.Deferred:
                await bus.SendAsync(MusicCatalogLookupAttempted.Deferred(
                    command.CommandId,
                    command.MusicCatalogId,
                    command.TargetProvider,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId,
                    admission.Reason,
                    admission.RetryAt,
                    admission.RetryAfterSecondsFrom(command.CreatedAt)), cancellationToken);
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
