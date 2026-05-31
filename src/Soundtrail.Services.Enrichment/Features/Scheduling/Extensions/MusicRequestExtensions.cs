using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Extensions
{
    public static class MusicRequestExtensions
    {
        public static LookupMusicCommand ToCommand(this LookupMusicRequest request, MusicCatalogId musicCatalogId)
        {
            return new LookupMusicCommand(
                CommandId: Guid.NewGuid().ToString("N"),
                MusicCatalogId: musicCatalogId,
                CreatedAt: request.OccurredAt,
                CorrelationId: request.CorrelationId);
        }
    }
}
