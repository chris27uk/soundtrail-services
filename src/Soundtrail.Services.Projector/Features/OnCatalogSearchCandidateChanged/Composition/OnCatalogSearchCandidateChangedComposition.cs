using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Composition;

public sealed class OnCatalogSearchCandidateChangedPorts(
    Func<IServiceProvider, IStoreCatalogSearchCandidatePort> searchCandidate)
{
    public Func<IServiceProvider, IStoreCatalogSearchCandidatePort> SearchCandidate { get; } = searchCandidate;
}

public static class OnCatalogSearchCandidateChangedComposition
{
    public static void Configure(IServiceCollection services, OnCatalogSearchCandidateChangedPorts ports)
    {
        services.TryAddSingleton(ports.SearchCandidate);
    }
}
