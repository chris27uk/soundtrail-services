using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Features.GetAlbumsForArtist.Support;
using Soundtrail.Services.Tests.Integration.Features.GetAlbumsForArtist;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support.DependencyInjection;

internal sealed class GetAlbumsForArtistTestAdapter(GetAlbumsForArtistPorts ports) : ISociableFeature, IApiFeature
{
    public static GetAlbumsForArtistTestAdapter Default() => new(DefaultPorts());

    public static GetAlbumsForArtistTestAdapter With(
        Func<GetAlbumsForArtistPorts, GetAlbumsForArtistPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetAlbumsForArtistPorts DefaultPorts() =>
        new(
            sp => GetAlbumsForArtistPortFake.Create(
                (artistId, cancellationToken) =>
                    TestPortResolution.RequireFake<IStoreArtistCatalogReadModelPort, StoreArtistCatalogReadModelPortFake>(sp)
                        .ReadAlbumsAsync(artistId, cancellationToken)),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => DiscoveryFeedbackPortFake.Create(
                (target, _) => Task.FromResult(
                    TestPortResolution.RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(sp)
                        .Read(target.NormalisedIdentifier))));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetAlbumsForArtistComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
