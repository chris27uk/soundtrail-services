using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForArtist.Support;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForArtist;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support.DependencyInjection;

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
