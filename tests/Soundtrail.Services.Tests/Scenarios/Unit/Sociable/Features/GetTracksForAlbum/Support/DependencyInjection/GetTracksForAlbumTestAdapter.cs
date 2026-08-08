using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForAlbum.Support;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForAlbum;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support.DependencyInjection;

internal sealed class GetTracksForAlbumTestAdapter(GetTracksForAlbumPorts ports) : ISociableFeature, IApiFeature
{
    public static GetTracksForAlbumTestAdapter Default() => new(DefaultPorts());

    public static GetTracksForAlbumTestAdapter With(
        Func<GetTracksForAlbumPorts, GetTracksForAlbumPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetTracksForAlbumPorts DefaultPorts() =>
        new(
            sp => GetTracksForAlbumPortFake.Create(
                (albumId, cancellationToken) =>
                    TestPortResolution.RequireFake<IStoreArtistCatalogReadModelPort, StoreArtistCatalogReadModelPortFake>(sp)
                        .ReadAlbumTracksAsync(albumId, cancellationToken)),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => DiscoveryFeedbackPortFake.Create(
                (target, _) => Task.FromResult(
                    TestPortResolution.RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(sp)
                        .Read(target.NormalisedIdentifier))));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetTracksForAlbumComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
