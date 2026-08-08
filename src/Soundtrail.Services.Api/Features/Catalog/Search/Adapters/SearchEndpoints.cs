using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Api.Shared.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/catalog/search",
            async (
                string? query,
                string? filter,
                IApiHandler<SearchRequest, SearchResponse?> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    DiscoveryResponseHeaders.Apply(httpContext, null);
                    return Results.BadRequest();
                }

                if (!Enum.TryParse<SearchType>(filter, true, out var resolvedFilter))
                {
                    DiscoveryResponseHeaders.Apply(httpContext, null);
                    return Results.BadRequest();
                }

                var response = await handler.Handle(new SearchRequest(query, resolvedFilter), cancellationToken);
                var dto = response is null ? null : typeRegistry.ToDto<SearchResponseDto>(response);
                DiscoveryResponseHeaders.Apply(httpContext, dto?.Discovery);
                return response is null ? Results.NotFound() : Results.Ok(dto);
            });

        return endpoints;
    }
}
