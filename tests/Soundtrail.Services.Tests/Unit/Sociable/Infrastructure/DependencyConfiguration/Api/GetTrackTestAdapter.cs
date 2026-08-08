using Microsoft.Extensions.Configuration;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.GetTrack.Api.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Api;

internal sealed class GetTrackTestAdapter(GetTrackPorts ports) : ISociableFeature, IApiFeature
{
    public static GetTrackTestAdapter Default() => new(DefaultPorts());

    public static GetTrackTestAdapter With(Func<GetTrackPorts, GetTrackPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetTrackPorts DefaultPorts() => new(_ => new GetTrackPortFake());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetTrackComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
