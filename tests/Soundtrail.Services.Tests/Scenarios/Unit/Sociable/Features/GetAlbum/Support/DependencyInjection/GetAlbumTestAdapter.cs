using Microsoft.Extensions.Configuration;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Features.GetAlbum.Support;
using Soundtrail.Services.Tests.Integration.Features.GetAlbum;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbum.Support.DependencyInjection;

internal sealed class GetAlbumTestAdapter(GetAlbumPorts ports) : ISociableFeature, IApiFeature
{
    public static GetAlbumTestAdapter Default() => new(DefaultPorts());

    public static GetAlbumTestAdapter With(Func<GetAlbumPorts, GetAlbumPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetAlbumPorts DefaultPorts() =>
        new(
            _ => new GetAlbumPortFake(),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetAlbumComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
