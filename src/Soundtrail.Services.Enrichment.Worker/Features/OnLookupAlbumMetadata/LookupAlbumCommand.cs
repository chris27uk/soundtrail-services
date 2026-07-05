using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata
{
    public record LookupAlbumCommand(
        CommandId CommandId,
        EnrichmentQuery Query,
        DateTime CreatedAt,
        CorrelationId CorrelationId);
}
