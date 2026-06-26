using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Pipeline;

public sealed class LookupMusicMetadataExecutionAdmissionDecorator(
    ILookupExecutionAdmissionPort executionAdmissionPort,
    ILookupMusicMetadataHandler inner) : ILookupMusicMetadataHandler
{
    public async Task<MusicCatalogLookupAttempted> Handle(
        LookupMusicMetadataCommand command,
        CancellationToken cancellationToken = default)
    {
        var admission = await executionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(
                ProviderName.MusicBrainz,
                command.CommandId,
                command.CreatedAt),
            cancellationToken);

        switch (admission.Status)
        {
            case LookupExecutionAdmissionStatus.Duplicate:
                return MusicCatalogLookupAttempted.Duplicate(
                    command.CommandId,
                    command.MusicCatalogId,
                    ProviderName.MusicBrainz,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId);
            case LookupExecutionAdmissionStatus.Deferred:
                return MusicCatalogLookupAttempted.Deferred(
                    command.CommandId,
                    command.MusicCatalogId,
                    ProviderName.MusicBrainz,
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
