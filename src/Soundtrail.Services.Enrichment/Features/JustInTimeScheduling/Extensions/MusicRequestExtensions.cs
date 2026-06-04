using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Queuing;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Features.JustInTimeScheduling.Extensions
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
