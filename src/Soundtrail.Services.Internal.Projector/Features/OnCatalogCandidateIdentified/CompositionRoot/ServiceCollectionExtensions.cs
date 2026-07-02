using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnCatalogCandidateIdentifiedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<DispatchAssessmentForCatalogCandidateIdentifiedHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CatalogCandidateIdentifiedToAssessmentSubscriptionHostedService>());
        return services;
    }
}
