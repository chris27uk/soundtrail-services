using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Features.GetTrack.Support;
using Soundtrail.Services.Tests.Integration.Features.GetTrack;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTrack.Support.DependencyInjection;

internal sealed class GetTrackTestAdapter(GetTrackPorts ports) : ISociableFeature, IApiFeature
{
    public static GetTrackTestAdapter Default() => new(DefaultPorts());

    public static GetTrackTestAdapter With(Func<GetTrackPorts, GetTrackPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetTrackPorts DefaultPorts() =>
        new(
            _ => new GetTrackPortFake(),
            sp => new ClockFake(sp.GetRequiredService<SociableScenarioOptions>().UtcNow),
            sp => sp.GetRequiredService<ICommandBus>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetTrackComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
