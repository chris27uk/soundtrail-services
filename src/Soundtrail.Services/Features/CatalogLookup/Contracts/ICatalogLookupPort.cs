using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Features.CatalogLookup.Contracts;

public interface ICatalogLookupPort
{
    Task<Track?> LookupAsync(
        CatalogLookupRequest request,
        CancellationToken cancellationToken);

    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
