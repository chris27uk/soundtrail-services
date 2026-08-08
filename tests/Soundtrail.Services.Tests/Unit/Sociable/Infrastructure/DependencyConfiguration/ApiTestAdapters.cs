using Microsoft.Extensions.Configuration;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;

internal sealed class ApiTestAdapters(GetTracksForPlaylistPorts ports) : IApiFeature
{
    public static ApiTestAdapters Default() =>
        new(CreateDefaultPorts());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetTracksForPlaylistComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }

    private static GetTracksForPlaylistPorts CreateDefaultPorts() =>
        new(
            sp => GetTracksForPlaylistPortFake.Create(
                (playlistId, cancellationToken) =>
                    TestPortResolution.RequireFake<IStorePlaylistTracksReadModelPort, StorePlaylistTracksReadModelPortFake>(sp)
                        .ReadAsync(playlistId, cancellationToken)),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>());
}
