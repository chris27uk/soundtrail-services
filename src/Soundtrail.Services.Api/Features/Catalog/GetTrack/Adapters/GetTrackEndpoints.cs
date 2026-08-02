using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;

public static class GetTrackEndpoints
{
    public static IEndpointRouteBuilder MapGetTrackEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/tracks/{trackId}",
            async (string trackId, IApiHandler<GetTrackRequest, GetTrackResponse?> handler, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var request = new GetTrackRequest(TrackId.From(trackId));
                var response = await handler.Handle(request, cancellationToken);
                var dto = response is null ? null : typeRegistry.ToDto<GetTrackResponseDto>(response);
                DiscoveryResponseHeaders.Apply(httpContext, dto?.Discovery);
                return response is null ? Results.NotFound() : Results.Ok(dto);
            });

        return endpoints;
    }
}
