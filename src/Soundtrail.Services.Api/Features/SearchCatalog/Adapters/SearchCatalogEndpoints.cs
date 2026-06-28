using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Api.Features.SearchCatalog.Adapters;

public static class SearchCatalogEndpoints
{
    public static IEndpointRouteBuilder MapSearchCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/search",
            async (
                string? q,
                string? types,
                string? playback,
                int? limit,
                int? offset,
                IApiHandler<SearchCatalogCommand, SearchCatalogResponse> handler,
                CancellationToken cancellationToken) =>
            {
                SearchCatalogCommand request;

                try
                {
                    request = new SearchCatalogCommand(
                        MusicIdentityText.NormalizeFreeText(q ?? throw new ArgumentException("Query is required.", nameof(q))),
                        SearchTypesFilter.Parse(types),
                        PlaybackProviderFilter.Parse(playback),
                        SearchLimit.From(limit),
                        SearchOffset.From(offset));
                }
                catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                var response = await handler.Handle(request, cancellationToken);
                return Results.Ok(TypeTranslationRegistry.Default.ToDto<Soundtrail.Contracts.Api.SearchCatalogResponseDto>(response));
            });

        return endpoints;
    }
}
