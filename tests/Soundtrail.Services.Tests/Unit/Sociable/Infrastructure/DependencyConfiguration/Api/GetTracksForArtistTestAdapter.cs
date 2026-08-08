using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Integration.GetTracksForArtist.Api.Ports;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Api;

internal sealed class GetTracksForArtistTestAdapter(GetTracksForArtistPorts ports) : ISociableFeature, IApiFeature
{
    public static GetTracksForArtistTestAdapter Default() => new(DefaultPorts());

    public static GetTracksForArtistTestAdapter With(
        Func<GetTracksForArtistPorts, GetTracksForArtistPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetTracksForArtistPorts DefaultPorts() =>
        new(
            sp => GetTracksForArtistPortFake.Create(
                (artistId, cancellationToken) =>
                    TestPortResolution.RequireFake<IStoreArtistCatalogReadModelPort, StoreArtistCatalogReadModelPortFake>(sp)
                        .ReadAsync(artistId, cancellationToken)),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => DiscoveryFeedbackPortFake.Create(
                (target, _) => Task.FromResult(
                    TestPortResolution.RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(sp)
                        .Read(target.NormalisedIdentifier))));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetTracksForArtistComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
