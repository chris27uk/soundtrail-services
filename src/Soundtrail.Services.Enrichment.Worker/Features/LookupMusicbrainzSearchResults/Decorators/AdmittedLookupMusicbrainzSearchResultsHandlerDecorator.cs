using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;

public sealed class AdmittedLookupMusicbrainzSearchResultsHandlerDecorator(
    IHandler<LookupMusicbrainzSearchResultsMessage> inner,
    ICommandBus commandBus,
    ILookupExecutionAdmissionPort lookupExecutionAdmissionPort,
    IClockPort clock) : IHandler<LookupMusicbrainzSearchResultsMessage>
{
    public async Task Handle(LookupMusicbrainzSearchResultsMessage request, CancellationToken cancellationToken = default)
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
                        CreateExistingPlaceholder(),
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

    private static LookupResultContext CreateContext(LookupMusicbrainzSearchResultsMessage request) =>
        new(CatalogWorkId.From(request.SearchCriteria), request.Id);

    private static CatalogItem CreateExistingPlaceholder() =>
        new CatalogItem.MusicArtist(new Domain.Catalog.Artists.Artist
        {
            Id = Domain.Catalog.Artists.ArtistId.From("musicbrainz-duplicate"),
            Name = Domain.Catalog.Artists.ArtistName.From("Duplicate")
        });
}
