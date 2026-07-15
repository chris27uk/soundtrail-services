using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.GetTrack.Adapters;

public static class GetTrackEndpoints
{
    public static IEndpointRouteBuilder MapGetTrackEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/tracks/{trackId}",
            async (string trackId, IApiHandler<GetTrackRequest, GetTrackResponse?> handler, CancellationToken cancellationToken) =>
            {
                var request = new GetTrackRequest(TrackId.From(trackId));
                var response = await handler.Handle(request, cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
