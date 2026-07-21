using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetArtist;

public sealed class GetArtistHandler(IGetArtistPort getArtistPort) : IApiHandler<GetArtistRequest, GetArtistResponse?>
{
    public async Task<GetArtistResponse?> Handle(GetArtistRequest request, CancellationToken cancellationToken = default) =>
        await getArtistPort.GetArtistAsync(request.ArtistId, cancellationToken);
}
