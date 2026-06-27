using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Api.Features.RequestKnownCatalogItem.Ports;

namespace Soundtrail.Services.Api.Features.RequestKnownCatalogItem.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRequestKnownCatalogItemFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IQueueKnownCatalogItemRequestPort>(sp => sp.GetRequiredService<IEnqueueKnownCatalogItemRequest>());
        services.TryAddScoped<RequestKnownCatalogItemHandler>();
        return services;
    }
}
