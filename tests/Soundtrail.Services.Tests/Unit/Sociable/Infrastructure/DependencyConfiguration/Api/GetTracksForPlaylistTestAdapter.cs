using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Api;

internal sealed class GetTracksForPlaylistTestAdapter(GetTracksForPlaylistPorts ports) : ISociableFeature, IApiFeature
{
    public static GetTracksForPlaylistTestAdapter Default() => new(DefaultPorts());

    public static GetTracksForPlaylistTestAdapter With(
        Func<GetTracksForPlaylistPorts, GetTracksForPlaylistPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetTracksForPlaylistPorts DefaultPorts() =>
        new(
            sp => GetTracksForPlaylistPortFake.Create(
                (playlistId, cancellationToken) =>
                    TestPortResolution.RequireFake<IStorePlaylistTracksReadModelPort, StorePlaylistTracksReadModelPortFake>(sp)
                        .ReadAsync(playlistId, cancellationToken)),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetTracksForPlaylistComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
