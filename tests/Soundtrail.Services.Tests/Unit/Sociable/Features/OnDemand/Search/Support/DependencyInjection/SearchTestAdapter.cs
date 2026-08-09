using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.Search.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Features.Search.Support;
using Soundtrail.Services.Tests.Integration.Features.Search;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support.DependencyInjection;

internal sealed class SearchTestAdapter(SearchPorts ports) : ISociableFeature, IApiFeature
{
    public static SearchTestAdapter Default() => new(DefaultPorts());

    public static SearchTestAdapter With(Func<SearchPorts, SearchPorts> customize) =>
        new(customize(DefaultPorts()));

    public static SearchPorts DefaultPorts() =>
        new(
            sp => SearchPortFake.Create(
                (criteria, cancellationToken) =>
                    TestPortResolution.RequireFake<IStoreCatalogSearchCandidatePort, StoreCatalogSearchCandidatePortFake>(sp)
                        .SearchAsync(criteria, cancellationToken)),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => DiscoveryFeedbackPortFake.Create(
                (target, _) => Task.FromResult(
                    TestPortResolution.RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(sp)
                        .Read(target.NormalisedIdentifier))));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        SearchComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
