using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Composition;

public sealed record OnCatalogSearchCandidateChangedPorts(
    Func<IServiceProvider, IStoreCatalogSearchCandidatePort> SearchCandidate);

public static class OnCatalogSearchCandidateChangedComposition
{
    public static void Configure(IServiceCollection services, OnCatalogSearchCandidateChangedPorts ports)
    {
        services.TryAddSingleton(ports.SearchCandidate);
    }
}
