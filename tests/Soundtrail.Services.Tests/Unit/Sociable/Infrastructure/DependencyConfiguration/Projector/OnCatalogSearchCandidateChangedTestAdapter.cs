using Microsoft.Extensions.Configuration;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Composition;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Projector;

internal sealed class OnCatalogSearchCandidateChangedTestAdapter(OnCatalogSearchCandidateChangedPorts ports)
    : ISociableFeature, IProjectorFeature
{
    public static OnCatalogSearchCandidateChangedTestAdapter Default() => new(DefaultPorts());

    public static OnCatalogSearchCandidateChangedTestAdapter With(
        Func<OnCatalogSearchCandidateChangedPorts, OnCatalogSearchCandidateChangedPorts> customize) =>
        new(customize(DefaultPorts()));

    public static OnCatalogSearchCandidateChangedPorts DefaultPorts() =>
        new(_ => new StoreCatalogSearchCandidatePortFake());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        OnCatalogSearchCandidateChangedComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
