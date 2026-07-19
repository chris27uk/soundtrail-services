using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Api.Features.Search.Adapters;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints, ITypeRegistry typeRegistry)
    {
        endpoints.MapGet(
            "/search",
            async (
                string? query,
                string? filter,
                IApiHandler<SearchRequest, SearchResponse?> handler,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest();
                }

                if (!Enum.TryParse<SearchType>(filter, true, out var resolvedFilter))
                {
                    return Results.BadRequest();
                }

                var response = await handler.Handle(new SearchRequest(query, resolvedFilter), cancellationToken);
                return response is null ? Results.NotFound() : Results.Ok(typeRegistry.ToDto(response));
            });

        return endpoints;
    }
}
