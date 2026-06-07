using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Extensions
{
    public static class MusicRequestExtensions
    {
        public static LookupMusicCommand ToCommand(
            this LookupMusicRequest request,
            MusicCatalogId musicCatalogId,
            LookupPriorityBand priority)
        {
            return new LookupMusicCommand(
                CommandId: CommandId.For(musicCatalogId.Value),
                MusicCatalogId: musicCatalogId,
                Priority: priority,
                CreatedAt: request.OccurredAt,
                CorrelationId: request.CorrelationId);
        }
    }
}
