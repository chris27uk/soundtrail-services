using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Extensions
{
    public static class MusicRequestExtensions
    {
        public static LookupMusicCommand ToCommand(
            this LookupMusicRequest request,
            MusicCatalogId musicCatalogId,
            LookupPriorityBand priority)
        {
            return new LookupMusicCommand(
                CommandId: Guid.NewGuid().ToString("N"),
                MusicCatalogId: musicCatalogId,
                Priority: priority,
                CreatedAt: request.OccurredAt,
                CorrelationId: request.CorrelationId);
        }
    }
}
