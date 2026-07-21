using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;

public sealed class IdempotentLookupMusicbrainzSearchResultsHandlerDecorator(
    IHandler<LookupMusicbrainzSearchResultsMessage> inner,
    ILookupExecutionReceiptStore lookupExecutionReceiptStore,
    ICommandBus commandBus,
    IClockPort clock) : IHandler<LookupMusicbrainzSearchResultsMessage>
{
    public async Task Handle(LookupMusicbrainzSearchResultsMessage request, CancellationToken cancellationToken = default)
    {
        var observedAt = clock.UtcNow;

        await using var session = await IdempotencySession.StartAsync(
            lookupExecutionReceiptStore,
            request.Id,
            cancellationToken);

        if (session.ProcessedBefore)
        {
            await commandBus.SendAsync(
                new CatalogLookupCompleted(
                    MessageId.New(),
                    request.RequestedAt,
                    request.CorrelationId,
                    new LookupResult.Duplicate(
                        CreateContext(request),
                        CreateExistingPlaceholder(),
                        "Lookup already completed.",
                        observedAt)),
                cancellationToken);
            return;
        }

        try
        {
            await inner.Handle(request, cancellationToken);
            await session.CompleteAsync(cancellationToken);
        }
        catch (LookupExecutionShortCircuitException)
        {
            await session.ReleaseAsync(cancellationToken);
        }
        catch
        {
            await session.ReleaseAsync(cancellationToken);
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
