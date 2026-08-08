using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;

public sealed class LookupMusicbrainzSearchResultsHandler(
    IReadCatalogEntriesBySearchCriteriaPort readCatalogEntriesBySearchCriteriaPort,
    IClockPort clock,
    ICommandBus commandBus) : IHandler<LookupMusicbrainzSearchResultsMessage>
{
    public async Task Handle(IncomingMessage<LookupMusicbrainzSearchResultsMessage> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var entries = await readCatalogEntriesBySearchCriteriaPort.ReadAsync(request.SearchCriteria, cancellationToken);
        var observedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new CatalogLookupCompleted(
                MessageId.New(),
                request.RequestedAt,
                request.CorrelationId,
                new LookupResult.Succeeded(
                    new LookupResultContext(
                        CatalogWorkId.From(request.SearchCriteria),
                        request.Id),
                    new LookedUpData.CatalogEntries(entries),
                    observedAt)),
            cancellationToken);
    }
}
