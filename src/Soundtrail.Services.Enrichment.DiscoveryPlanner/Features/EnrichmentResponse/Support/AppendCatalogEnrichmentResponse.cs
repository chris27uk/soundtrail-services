using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;

public sealed class AppendCatalogEnrichmentResponse(IMusicTrackEventRepository eventRepository)
{
    public async Task<IReadOnlyList<IMusicTrackEvent>> AppendAsync(
        Soundtrail.Domain.Responses.EnrichmentResponse response,
        CancellationToken cancellationToken)
    {
        var aggregate = await CatalogEntityAggregate.LoadAsync(
            eventRepository,
            response.MusicCatalogId,
            cancellationToken);
        aggregate.RecordEnrichmentResponse(response);

        var append = await aggregate.SaveAsync(
            eventRepository,
            response.CommandId,
            cancellationToken);

        return append.Appended ? append.AppendedEvents : Array.Empty<IMusicTrackEvent>();
    }
}
