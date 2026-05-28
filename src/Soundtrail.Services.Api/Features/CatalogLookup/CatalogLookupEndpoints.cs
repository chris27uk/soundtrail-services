namespace Soundtrail.Services.Api.Features.CatalogLookup;

public static class CatalogLookupEndpoints
{
    public static IEndpointRouteBuilder MapCatalogLookupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/lookup", () => Results.StatusCode(StatusCodes.Status501NotImplemented));
        return endpoints;
    }
}
