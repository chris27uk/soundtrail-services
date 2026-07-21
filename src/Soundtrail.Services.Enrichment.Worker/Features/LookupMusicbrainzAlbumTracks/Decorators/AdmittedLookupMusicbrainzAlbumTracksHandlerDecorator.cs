using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;

public sealed class AdmittedLookupMusicbrainzAlbumTracksHandlerDecorator(
    IHandler<LookupMusicbrainzAlbumTracksMessage> inner,
    ICommandBus commandBus,
    ILookupExecutionAdmissionPort lookupExecutionAdmissionPort,
    IClockPort clock) : IHandler<LookupMusicbrainzAlbumTracksMessage>
{
    public async Task Handle(LookupMusicbrainzAlbumTracksMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;
        var admissionResult = await lookupExecutionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.MusicBrainz, request.Id, observedAt),
            cancellationToken);

        if (admissionResult.Status == LookupExecutionAdmissionStatus.Duplicate)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Duplicate(
                        CreateContext(request),
                        new CatalogItem.MusicAlbum(new Album(request.AlbumId, null, null, null, null, observedAt)),
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

    private static LookupResultContext CreateContext(LookupMusicbrainzAlbumTracksMessage request) =>
        new(CatalogWorkId.From(new CatalogItemOperation.ChildTracksForAlbum(request.AlbumId)), request.Id);
}
