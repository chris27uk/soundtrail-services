using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;

public sealed class AdmittedLookupPlaylistTracksByProviderHandlerDecorator(
    IHandler<LookupPlaylistTracksByProviderMessage> inner,
    ICommandBus commandBus,
    ILookupExecutionAdmissionPort lookupExecutionAdmissionPort,
    IClockPort clock) : IHandler<LookupPlaylistTracksByProviderMessage>
{
    public async Task Handle(LookupPlaylistTracksByProviderMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;
        var admissionRequest = new LookupExecutionAdmissionRequest(
            LookupSource.Kworb,
            request.Id,
            observedAt);

        var admissionResult = await lookupExecutionAdmissionPort.TryAcquireAsync(admissionRequest, cancellationToken);

        if (admissionResult.Status == LookupExecutionAdmissionStatus.Duplicate)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Duplicate(
                        CreateContext(request),
                        CreateExistingPlaylistItem(),
                        "Lookup already executing.",
                        observedAt)),
                cancellationToken);
            throw new LookupExecutionShortCircuitException();
        }

        if (admissionResult.Status == LookupExecutionAdmissionStatus.Deferred)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Deferred(
                        CreateContext(request),
                        admissionResult.RetryAt ?? observedAt.AddMinutes(1),
                        admissionResult.Reason,
                        observedAt)),
                cancellationToken);
            throw new LookupExecutionShortCircuitException();
        }

        try
        {
            await inner.Handle(request, cancellationToken);
            await lookupExecutionAdmissionPort.CommitAsync(request.Id, cancellationToken);
        }
        catch
        {
            await lookupExecutionAdmissionPort.ReleaseAsync(request.Id, cancellationToken);
            throw;
        }
    }

    private static LookupResultContext CreateContext(LookupPlaylistTracksByProviderMessage request) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.ChildTracksForPlaylist(request.PlaylistId)),
            request.Id);

    private static CatalogItem CreateExistingPlaylistItem() =>
        new CatalogItem.MusicPlaylist(new Playlist());
}
