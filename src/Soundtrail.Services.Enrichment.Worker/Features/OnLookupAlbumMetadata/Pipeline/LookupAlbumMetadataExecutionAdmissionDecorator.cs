using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Pipeline;

public sealed class LookupAlbumMetadataExecutionAdmissionDecorator(
    ILookupExecutionAdmissionPort executionAdmissionPort,
    IHandler<LookupAlbumMetadataCommand> inner,
    ICommandBus bus) : IHandler<LookupAlbumMetadataCommand>
{
    public async Task Handle(LookupAlbumMetadataCommand command, CancellationToken cancellationToken = default)
    {
        var admission = await executionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(
                LookupSource.MusicBrainz,
                command.CommandId,
                command.CreatedAt),
            cancellationToken);

        if (admission.Status == LookupExecutionAdmissionStatus.Duplicate)
        {
            await bus.SendAsync(
                AlbumMetadataLookupAttempted.Duplicate(
                    command.CommandId,
                    command.ArtistId,
                    command.AlbumId,
                    LookupSource.MusicBrainz,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId),
                cancellationToken);
            return;
        }

        if (admission.Status == LookupExecutionAdmissionStatus.Deferred)
        {
            await bus.SendAsync(
                AlbumMetadataLookupAttempted.Deferred(
                    command.CommandId,
                    command.ArtistId,
                    command.AlbumId,
                    LookupSource.MusicBrainz,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId,
                    admission.Reason,
                    admission.RetryAt,
                    admission.RetryAfterSecondsFrom(command.CreatedAt)),
                cancellationToken);
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
