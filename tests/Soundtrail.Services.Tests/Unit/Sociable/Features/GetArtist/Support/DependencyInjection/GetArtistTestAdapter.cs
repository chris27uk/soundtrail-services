using Microsoft.Extensions.Configuration;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Composition;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Features.GetArtist.Support;
using Soundtrail.Services.Tests.Integration.Features.GetArtist;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetArtist.Support.DependencyInjection;

internal sealed class GetArtistTestAdapter(GetArtistPorts ports) : ISociableFeature, IApiFeature
{
    public static GetArtistTestAdapter Default() => new(DefaultPorts());

    public static GetArtistTestAdapter With(Func<GetArtistPorts, GetArtistPorts> customize) =>
        new(customize(DefaultPorts()));

    public static GetArtistPorts DefaultPorts() => new(_ => new GetArtistPortFake());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        GetArtistComposition.Configure(services, ports);

    public void ConfigureApplication(WebApplication app)
    {
    }
}
