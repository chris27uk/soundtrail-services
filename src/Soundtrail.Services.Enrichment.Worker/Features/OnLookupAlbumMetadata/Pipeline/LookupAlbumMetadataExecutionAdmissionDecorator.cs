using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Pipeline;

public sealed class LookupAlbumMetadataExecutionAdmissionDecorator(
    ILookupExecutionAdmissionPort executionAdmissionPort,
    IHandler<LookupAlbumCommand> inner,
    ICommandBus bus) : IHandler<LookupAlbumCommand>
{
    public async Task Handle(LookupAlbumCommand command, CancellationToken cancellationToken = default)
    {
        var admission = await executionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(
                LookupSource.MusicBrainz,
                command.CommandId,
                command.CreatedAt),
            cancellationToken);

        admission.Status switch
        {
            LookupExecutionAdmissionStatus.Duplicate => await bus.SendAsync(new CatalogLookupCompleted(new LookupResult.Duplicate()), cancellationToken);
            LookupExecutionAdmissionStatus.Deferred => 
                await bus.SendAsync(
                    new CatalogLookupCompleted(new LookupResult.Deferred(
                        command.CommandId,
                        command.ArtistId,
                        command.AlbumId,
                        LookupSource.MusicBrainz,
                        command.Priority,
                        command.CreatedAt,
                        command.CorrelationId,
                        admission.Reason,
                        admission.RetryAt),
                    cancellationToken);
                return;

            default:
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
                break;
        }
    }
}
