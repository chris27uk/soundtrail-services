using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Features.CatalogLookup;

public sealed class CatalogLookupHandler(ICatalogLookupPort catalogLookupPort)
{
    public Task<Track?> Handle(
        CatalogLookupRequest request,
        CancellationToken cancellationToken = default) =>
        catalogLookupPort.LookupAsync(request, cancellationToken);
}
