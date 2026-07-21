using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;

public sealed class AdmittedLookupStreamingLocationByIsrcHandlerDecorator(
    IHandler<LookupStreamingLocationByIsrcMessage> inner,
    ICommandBus commandBus,
    ILookupExecutionAdmissionPort lookupExecutionAdmissionPort,
    IClockPort clock) : IHandler<LookupStreamingLocationByIsrcMessage>
{
    public async Task Handle(LookupStreamingLocationByIsrcMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;
        var admissionResult = await lookupExecutionAdmissionPort.TryAcquireAsync(
            new LookupExecutionAdmissionRequest(LookupSource.Odesli, request.Id, observedAt),
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
                        CreateExistingTrackItem(request.TrackId),
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

    private static LookupResultContext CreateContext(LookupStreamingLocationByIsrcMessage request) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.StreamingLocationForTrack(request.TrackId)),
            request.Id);

    private static CatalogItem CreateExistingTrackItem(TrackId trackId) =>
        new CatalogItem.MusicTrack(new Track(trackId));
}
