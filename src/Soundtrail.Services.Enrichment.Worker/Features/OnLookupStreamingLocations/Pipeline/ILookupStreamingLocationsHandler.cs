using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Pipeline;

public interface ILookupStreamingLocationsHandler
{
    Task<MusicCatalogLookupAttempted> Handle(
        LookupStreamingLocationsCommand command,
        CancellationToken cancellationToken = default);
}
